using System;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimationViewer
{
    #region Variables

    private static Animator _animator;
    private static AnimatorController _editorController;
    private static AnimatorState _newState;

    private static int _defaultAnimatorControllerLayers;
    private const string _debugStateName = "AnimationViewer State";
    private const string _debugLayerName = "AnimationViewer Layer";

    private static EditorCoroutine _playAnimationCoroutine;
    private static bool _pauseAnimation;

    #endregion

    #region System

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;

        CancelOperation();
    }

    public static void InitializeSystem(AnimationClip clip, Animator animator) //Get animator
    {
        if (clip == null || animator == null)
            return;

        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.quitting += CancelOperation;

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
        if (runtimeAnimator.GetType() == typeof(AnimatorOverrideController))
        {
            AnimatorOverrideController overrideController = (AnimatorOverrideController)runtimeAnimator;
            runtimeAnimator = overrideController.runtimeAnimatorController;
        }

        return runtimeAnimator;
    }

    private static void CreateLayer()
    {
        for (int i = 0; i < _editorController.layers.Length; i++)
        {
            if (_editorController.layers[i].name.Contains(_debugLayerName))
                return;
        }

        var layer = new AnimatorControllerLayer
        {
            name = _debugLayerName,
            defaultWeight = 1f,
            stateMachine = new AnimatorStateMachine()
        };

        _editorController.AddLayer(layer);
    }

    private static void CreateDefaultState(AnimationClip clip)
    {
        AnimatorStateMachine animatorStateMachine = _editorController.layers[_editorController.layers.Length - 1].stateMachine;

        for (int i = 0; i < animatorStateMachine.states.Length; i++)
        {
            if (animatorStateMachine.states[i].state.name.Contains(_debugStateName))
                return;
        }

        _newState = animatorStateMachine.AddState(_debugStateName);
        animatorStateMachine.defaultState = _newState;

        _editorController.SetStateEffectiveMotion(_newState, clip);
        _animator.Update(0);
    }

    private static void SaveDependencies(RuntimeAnimatorController runtimeAnimator)
    {
        _editorController = (AnimatorController)AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(runtimeAnimator), typeof(AnimatorController));
        _defaultAnimatorControllerLayers = _editorController.layers.Length;
    }

    private static void UpdateAnimator(float normalizedTime)
    {
        _animator.speed = 0f;
        _animator.Play(_debugStateName, 1, normalizedTime);
        _animator.Update(0);
    }

    public static void UpdateAnimation(float normalizedTime)
    {
        if (_animator == null)
            return;

        UpdateAnimator(normalizedTime);
    }

    public static void UpdateAnimation(int frame, AnimationClip animationClip)
    {
        if (_animator == null || animationClip == null)
            return;

        float normalizedTime = frame / (animationClip.frameRate * animationClip.length);
        UpdateAnimator(normalizedTime);
    }

    public static void PlayAnimation()
    {
        if (_playAnimationCoroutine != null)
        {
            EditorCoroutineUtility.StopCoroutine(_playAnimationCoroutine);
        }

        _playAnimationCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(PlayAnimationRoutine());
    }

    private static IEnumerator PlayAnimationRoutine()
    {
        _pauseAnimation = false;

        float timeStep = 0f;
        while (timeStep < 1f)
        {
            if (_animator == null)
                yield break;

            if (_pauseAnimation)
                yield return null;

            _animator.speed = 1f;
            _animator.Play(_debugStateName, 1, timeStep);
            _animator.Update(timeStep);
            yield return new WaitForSecondsRealtime(0.01f);
            timeStep += 0.01f;
        }

        _pauseAnimation = false;
        _playAnimationCoroutine = null;
    }

    public static void PauseAnimation()
    {
        if (_playAnimationCoroutine == null)
            return;

        _pauseAnimation = true;
    }

    public static void ResumeAnimation()
    {
        if (_playAnimationCoroutine == null)
            return;

        _pauseAnimation = false;
    }

    public static void CancelOperation()
    {
        if (_editorController == null || _editorController.layers.Length <= _defaultAnimatorControllerLayers)
            return;

        ResetSystem();

        OnOperationEnd?.Invoke();
    }

    private static bool HasRunningOperation(AnimationClip clip)
    {
        if (_editorController != null && _newState != null)
        {
            _editorController.SetStateEffectiveMotion(_newState, clip);
            _animator.Update(0);

            return true;
        }

        return false;
    }

    private static void ResetSystem()
    {
        int index = _editorController.layers.Length - 1;
        if (_editorController.layers[index].name != _debugLayerName)
            return;

        _editorController.RemoveLayer(index);
        _animator.speed = 1f;
        _animator.Update(0);

        _animator = null;
        _editorController = null;
        _newState = null;
        _playAnimationCoroutine = null;
        _defaultAnimatorControllerLayers = 0;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.quitting -= CancelOperation;
    }

    #endregion

    #region Events

    public static event Action OnOperationEnd;

    #endregion
}
