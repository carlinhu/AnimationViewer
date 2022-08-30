using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimationViewer
{
    #region Variables

    private static Animator _animator;
    private static AnimatorController _editorController;
    private static AnimatorState newState;

    private static int _defaultAnimatorControllerLayers;
    private const string _debugStateName = "AnimationViewer State";
    private const string _debugLayerName = "AnimationViewer Layer";

    private static Task task;
    private static bool _abortTask;

    #endregion

    #region System

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            CancelOperation();
    }

    public static void InitializeSystem(AnimationClip clip, Animator animator) //Get animator
    {
        if (clip == null || animator == null)
            return;

        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        _animator = animator;

        if (HasRunningOperation(clip))
            return;

        SaveDependencies(GetRuntimeController());

        CreateLayer();

        CreateDefaultState(clip);
    }

    private static RuntimeAnimatorController GetRuntimeController()
    {
        var runtimeAnimator = _animator.runtimeAnimatorController;
        if (runtimeAnimator.GetType() == typeof(AnimatorOverrideController)) // if it is an override controller we need to get the base controller
        {
            AnimatorOverrideController overrideController = (AnimatorOverrideController)runtimeAnimator;
            runtimeAnimator = overrideController.runtimeAnimatorController; // runtimeAnimatorController of an AnimatorOverrideController is the base controller
        }

        return runtimeAnimator;
    }

    private static void CreateLayer()
    {
        var layer = new AnimatorControllerLayer
        {
            name = _debugLayerName,
            defaultWeight = 1f,
            stateMachine = new AnimatorStateMachine() // Make sure to create a StateMachine as well, as a default one is not created
        };

        _editorController.AddLayer(layer);
    }

    private static void CreateDefaultState(AnimationClip clip)
    {
        AnimatorStateMachine animatorStateMachine = _editorController.layers[_editorController.layers.Length - 1].stateMachine;
        newState = animatorStateMachine.AddState(_debugStateName);
        animatorStateMachine.defaultState = newState;

        _editorController.SetStateEffectiveMotion(newState, clip);
        _animator.Update(0);
    }

    private static void SaveDependencies(RuntimeAnimatorController runtimeAnimator)
    {
        _editorController = (AnimatorController)AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(runtimeAnimator), typeof(AnimatorController));
        _defaultAnimatorControllerLayers = _editorController.layers.Length;
    }

    public static void UpdateAnimation(float normalizedTime)
    {
        _animator.speed = 0f;
        _animator.Play(_debugStateName, 1, normalizedTime);
        _animator.Update(0);
    }

    public static void PlayAnimation()
    {
        if (task != null)
        {
            _abortTask = true;
            task = null;
            return;
        }

        task = PlayAnimationRoutine();
    }

    private static async Task PlayAnimationRoutine()
    {
        _abortTask = false;
        float timeStep = 0f;
        while (timeStep < 1f)
        {
            if (_abortTask)
                break;

            _animator.speed = 1f;
            _animator.Play(_debugStateName, 1, timeStep);
            _animator.Update(0);
            await Task.Delay(1);
            timeStep += 0.01f;
        }
    }

    public static void CancelOperation()
    {
        if (_editorController == null || _editorController.layers.Length <= _defaultAnimatorControllerLayers)
            return;

        int index = _editorController.layers.Length - 1;
        if (_editorController.layers[index].name != _debugLayerName)
            return;

        _editorController.RemoveLayer(index);
        _animator.speed = 1f;
        _animator.Update(0);

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    private static bool HasRunningOperation(AnimationClip clip)
    {
        if (_editorController != null && newState != null)
        {
            _editorController.SetStateEffectiveMotion(newState, clip);
            _animator.Update(0);

            return true;
        }

        return false;
    }

    #endregion
}
