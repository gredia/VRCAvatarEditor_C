using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using nadena.dev.ndmf.preview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRCAvatarEditor.NDMF
{
    /// <summary>
    /// Applies the active NDMF preview pipeline to the avatar monitor camera.
    /// This class is compiled only when a compatible NDMF package is installed.
    /// </summary>
    public sealed class NDMFPreviewBridge : IDisposable
    {
        private static readonly MethodInfo ExcludeAvatarFromDefaultPreviewMethod =
            typeof(NDMFPreview).GetMethod(
                "ExcludeAvatarFromDefaultPreview",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        private sealed class TransformPair
        {
            public Transform Owner;
            public Transform Source;
        }

        private sealed class RendererPair
        {
            public Renderer Owner;
            public Renderer Source;
        }

        private readonly Camera camera;
        private readonly GameObject avatarRoot;
        private readonly Scene ownerScene;
        private readonly List<TransformPair> transformPairs = new List<TransformPair>();
        private readonly List<RendererPair> rendererPairs = new List<RendererPair>();

        private PreviewSession sourceSession;
        private PreviewSession previewSession;
        private Scene ndmfScene;
        private Scene editingScene;
        private GameObject sourceAvatarRoot;
        private IDisposable defaultPreviewExclusion;
        private bool isDisposed;

        public NDMFPreviewBridge(Camera camera, GameObject avatarRoot, Scene ownerScene)
        {
            this.camera = camera;
            this.avatarRoot = avatarRoot;
            this.ownerScene = ownerScene;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void PrepareForRender()
        {
            if (isDisposed || camera == null) return;

            if (avatarRoot == null || !ownerScene.IsValid() || !ownerScene.isLoaded)
            {
                ReleasePreviewState();
                camera.overrideSceneCullingMask = 0;
                return;
            }

            RefreshSession();
            SynchronizeSourceAvatar();

            var sceneCullingMask = EditorSceneManager.GetSceneCullingMask(ownerScene);
            if (previewSession != null && ndmfScene.IsValid() && ndmfScene.isLoaded)
            {
                sceneCullingMask |= EditorSceneManager.GetSceneCullingMask(ndmfScene);
            }

            camera.overrideSceneCullingMask = sceneCullingMask;
        }

        private void RefreshSession()
        {
            var currentSession = EditorApplication.isPlayingOrWillChangePlaymode
                ? null
                : PreviewSession.Current;
            var sourceAvatarIsReady =
                sourceAvatarRoot != null &&
                ndmfScene.IsValid() &&
                ndmfScene.isLoaded &&
                sourceAvatarRoot.scene == ndmfScene &&
                editingScene.IsValid() &&
                editingScene.isLoaded &&
                avatarRoot.scene == editingScene;

            if (ReferenceEquals(currentSession, sourceSession) &&
                (currentSession == null || (previewSession != null && sourceAvatarIsReady)))
            {
                return;
            }

            ReleasePreviewState();
            sourceSession = currentSession;

            if (sourceSession == null) return;

            CreateSourceAvatar();
            previewSession = sourceSession.Fork("VRC Avatar Editor avatar monitor");
            previewSession.HiddenRenderers = GetHiddenRenderers;
            previewSession.OverrideCamera(camera);
        }

        private void CreateSourceAvatar()
        {
            // EditorSceneManager.NewPreviewScene instances are not returned by
            // SceneManager.sceneCount, so NDMF filters cannot discover the editable
            // monitor avatar directly. Keep that avatar as the editing source and put
            // a synchronized copy in NDMF's managed scene instead.
            ndmfScene = NDMFPreviewSceneManager.GetPreviewScene();
            if (!ndmfScene.IsValid() || !ndmfScene.isLoaded)
            {
                throw new InvalidOperationException(
                    "Could not obtain the NDMF preview scene for the avatar monitor.");
            }

            sourceAvatarRoot = UnityEngine.Object.Instantiate(avatarRoot);
            sourceAvatarRoot.name = avatarRoot.name;
            sourceAvatarRoot.hideFlags = HideFlags.None;
            SceneManager.MoveGameObjectToScene(sourceAvatarRoot, ndmfScene);
            ExcludeSourceAvatarFromDefaultPreview();

            transformPairs.Clear();
            rendererPairs.Clear();
            BuildSynchronizationMap(avatarRoot.transform, sourceAvatarRoot.transform);
            SynchronizeSourceAvatar();

            // Keep the editable avatar alive but outside the camera's combined scene
            // culling mask. This prevents it from overlapping the synchronized copy
            // while the asynchronous NDMF proxy pipeline is being prepared.
            editingScene = EditorSceneManager.NewPreviewScene();
            if (!editingScene.IsValid())
            {
                throw new InvalidOperationException(
                    "Could not create the editing scene for the avatar monitor.");
            }
            SceneManager.MoveGameObjectToScene(avatarRoot, editingScene);
        }

        private void ExcludeSourceAvatarFromDefaultPreview()
        {
            // NDMF 1.14.4 keeps this helper internal. Its exclusion applies to the
            // global Scene View session, while PreviewSession.Fork intentionally does
            // not inherit that exclusion, which leaves this camera-specific fork active.
            if (ExcludeAvatarFromDefaultPreviewMethod == null)
            {
                throw new MissingMethodException(
                    typeof(NDMFPreview).FullName,
                    "ExcludeAvatarFromDefaultPreview");
            }

            defaultPreviewExclusion = ExcludeAvatarFromDefaultPreviewMethod.Invoke(
                null,
                new object[] { sourceAvatarRoot }) as IDisposable;
            if (defaultPreviewExclusion == null)
            {
                throw new InvalidOperationException(
                    "Could not exclude the avatar monitor source from the default NDMF preview.");
            }
        }

        private void BuildSynchronizationMap(Transform owner, Transform source)
        {
            transformPairs.Add(new TransformPair
            {
                Owner = owner,
                Source = source
            });

            var ownerRenderers = owner.GetComponents<Renderer>();
            var sourceRenderers = source.GetComponents<Renderer>();
            var rendererCount = Math.Min(ownerRenderers.Length, sourceRenderers.Length);
            for (var index = 0; index < rendererCount; index++)
            {
                rendererPairs.Add(new RendererPair
                {
                    Owner = ownerRenderers[index],
                    Source = sourceRenderers[index]
                });
            }

            var childCount = Math.Min(owner.childCount, source.childCount);
            for (var index = 0; index < childCount; index++)
            {
                BuildSynchronizationMap(owner.GetChild(index), source.GetChild(index));
            }
        }

        private void SynchronizeSourceAvatar()
        {
            if (sourceAvatarRoot == null) return;

            foreach (var pair in transformPairs)
            {
                if (pair.Owner == null || pair.Source == null) continue;

                if (pair.Source.localPosition != pair.Owner.localPosition)
                    pair.Source.localPosition = pair.Owner.localPosition;
                if (pair.Source.localRotation != pair.Owner.localRotation)
                    pair.Source.localRotation = pair.Owner.localRotation;
                if (pair.Source.localScale != pair.Owner.localScale)
                    pair.Source.localScale = pair.Owner.localScale;
                if (pair.Source.gameObject.activeSelf != pair.Owner.gameObject.activeSelf)
                    pair.Source.gameObject.SetActive(pair.Owner.gameObject.activeSelf);
            }

            foreach (var pair in rendererPairs)
            {
                if (pair.Owner == null || pair.Source == null) continue;

                pair.Source.enabled = pair.Owner.enabled;

                var ownerSkinnedMesh = pair.Owner as SkinnedMeshRenderer;
                var sourceSkinnedMesh = pair.Source as SkinnedMeshRenderer;
                if (ownerSkinnedMesh != null && sourceSkinnedMesh != null)
                {
                    if (sourceSkinnedMesh.sharedMesh != ownerSkinnedMesh.sharedMesh)
                        sourceSkinnedMesh.sharedMesh = ownerSkinnedMesh.sharedMesh;
                    if (sourceSkinnedMesh.localBounds != ownerSkinnedMesh.localBounds)
                        sourceSkinnedMesh.localBounds = ownerSkinnedMesh.localBounds;

                    var blendShapeCount = sourceSkinnedMesh.sharedMesh != null
                        ? sourceSkinnedMesh.sharedMesh.blendShapeCount
                        : 0;
                    for (var index = 0; index < blendShapeCount; index++)
                    {
                        var weight = ownerSkinnedMesh.GetBlendShapeWeight(index);
                        if (!Mathf.Approximately(sourceSkinnedMesh.GetBlendShapeWeight(index), weight))
                        {
                            sourceSkinnedMesh.SetBlendShapeWeight(index, weight);
                        }
                    }
                }

                var ownerMeshRenderer = pair.Owner as MeshRenderer;
                var sourceMeshRenderer = pair.Source as MeshRenderer;
                if (ownerMeshRenderer != null && sourceMeshRenderer != null)
                {
                    var ownerFilter = ownerMeshRenderer.GetComponent<MeshFilter>();
                    var sourceFilter = sourceMeshRenderer.GetComponent<MeshFilter>();
                    if (ownerFilter != null &&
                        sourceFilter != null &&
                        sourceFilter.sharedMesh != ownerFilter.sharedMesh)
                    {
                        sourceFilter.sharedMesh = ownerFilter.sharedMesh;
                    }
                }
            }
        }

        private ImmutableHashSet<Renderer> GetHiddenRenderers(ComputeContext context)
        {
            var sourceTransform = sourceAvatarRoot != null ? sourceAvatarRoot.transform : null;
            var renderersOutsideSourceAvatar = context.GetComponentsByType<Renderer>()
                .Where(renderer =>
                    renderer != null &&
                    (sourceTransform == null ||
                     (renderer.transform != sourceTransform &&
                      !renderer.transform.IsChildOf(sourceTransform))));
            var ownerAvatarRenderers = context.GetComponentsInChildren<Renderer>(
                avatarRoot,
                true);

            return renderersOutsideSourceAvatar
                .Concat(ownerAvatarRenderers)
                .Where(renderer => renderer != null)
                .ToImmutableHashSet();
        }

        private void ReleasePreviewState()
        {
            if (camera != null)
            {
                PreviewSession.ClearCameraOverride(camera);
            }

            previewSession?.Dispose();
            previewSession = null;
            sourceSession = null;

            defaultPreviewExclusion?.Dispose();
            defaultPreviewExclusion = null;

            if (sourceAvatarRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(sourceAvatarRoot);
            }

            if (avatarRoot != null &&
                ownerScene.IsValid() &&
                ownerScene.isLoaded &&
                avatarRoot.scene != ownerScene)
            {
                SceneManager.MoveGameObjectToScene(avatarRoot, ownerScene);
            }

            if (editingScene.IsValid() && editingScene.isLoaded)
            {
                EditorSceneManager.ClosePreviewScene(editingScene);
            }

            sourceAvatarRoot = null;
            ndmfScene = default;
            editingScene = default;
            transformPairs.Clear();
            rendererPairs.Clear();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode &&
                state != PlayModeStateChange.ExitingPlayMode)
            {
                return;
            }

            ReleasePreviewState();
            if (camera != null)
            {
                camera.overrideSceneCullingMask = 0;
            }
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            ReleasePreviewState();
            if (camera != null)
            {
                camera.overrideSceneCullingMask = 0;
            }
        }
    }
}
