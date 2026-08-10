using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRCAvatarEditor
{
    /// <summary>
    /// Connects the avatar monitor to the NDMF integration assembly while keeping
    /// NDMF implementation types out of the main editor assembly.
    /// </summary>
    internal sealed class OptionalNDMFPreview : IDisposable
    {
        private const string ImplementationTypeName =
            "VRCAvatarEditor.NDMF.NDMFPreviewBridge, VRCAvatarEditor.NDMF.Editor";
        private const string NDMFTypeName =
            "nadena.dev.ndmf.preview.PreviewSession, nadena.dev.ndmf";

        private static bool hasLoggedMissingIntegration;

        private Action prepareForRender;
        private Action finishRender;
        private Action disposeImplementation;
        private bool isDisposed;

        private OptionalNDMFPreview(
            Action prepareForRender,
            Action finishRender,
            Action disposeImplementation)
        {
            this.prepareForRender = prepareForRender;
            this.finishRender = finishRender;
            this.disposeImplementation = disposeImplementation;
        }

        public bool IsAvailable => !isDisposed && prepareForRender != null;

        public static OptionalNDMFPreview TryCreate(Camera camera, GameObject avatarRoot, Scene avatarScene)
        {
            var implementationType = Type.GetType(ImplementationTypeName, false);
            if (implementationType == null)
            {
                if (!hasLoggedMissingIntegration && Type.GetType(NDMFTypeName, false) != null)
                {
                    hasLoggedMissingIntegration = true;
                    Debug.LogWarning(
                        "[VRCAvatarEditor] NDMF was detected, but the avatar monitor integration " +
                        "could not be loaded. NDMF 1.14.4 or newer is required.");
                }

                return null;
            }

            try
            {
                var implementation = Activator.CreateInstance(
                    implementationType,
                    camera,
                    avatarRoot,
                    avatarScene);
                var prepareMethod = implementationType.GetMethod(
                    "PrepareForRender",
                    BindingFlags.Instance | BindingFlags.Public);
                var finishMethod = implementationType.GetMethod(
                    "FinishRender",
                    BindingFlags.Instance | BindingFlags.Public);
                var disposeMethod = implementationType.GetMethod(
                    "Dispose",
                    BindingFlags.Instance | BindingFlags.Public);

                if (prepareMethod == null || finishMethod == null || disposeMethod == null)
                {
                    throw new MissingMethodException(
                        implementationType.FullName,
                        prepareMethod == null
                            ? "PrepareForRender"
                            : finishMethod == null
                                ? "FinishRender"
                                : "Dispose");
                }

                var prepare = (Action)Delegate.CreateDelegate(
                    typeof(Action),
                    implementation,
                    prepareMethod);
                var finish = (Action)Delegate.CreateDelegate(
                    typeof(Action),
                    implementation,
                    finishMethod);
                var dispose = (Action)Delegate.CreateDelegate(
                    typeof(Action),
                    implementation,
                    disposeMethod);

                return new OptionalNDMFPreview(prepare, finish, dispose);
            }
            catch (Exception exception)
            {
                Debug.LogException(UnwrapInvocationException(exception));
                return null;
            }
        }

        public void FinishRender()
        {
            if (!IsAvailable) return;

            try
            {
                finishRender();
            }
            catch (Exception exception)
            {
                Debug.LogException(UnwrapInvocationException(exception));
                Dispose();
            }
        }

        public void PrepareForRender()
        {
            if (!IsAvailable) return;

            try
            {
                prepareForRender();
            }
            catch (Exception exception)
            {
                Debug.LogException(UnwrapInvocationException(exception));
                Dispose();
            }
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;

            try
            {
                disposeImplementation?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(UnwrapInvocationException(exception));
            }
            finally
            {
                prepareForRender = null;
                finishRender = null;
                disposeImplementation = null;
            }
        }

        private static Exception UnwrapInvocationException(Exception exception)
        {
            var invocationException = exception as TargetInvocationException;
            return invocationException?.InnerException ?? exception;
        }
    }
}
