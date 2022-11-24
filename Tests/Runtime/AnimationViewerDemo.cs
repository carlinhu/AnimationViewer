using UnityEditor;
using UnityEngine;

public class AnimationViewerDemo : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private AnimationClip _animationClip;

    private bool _systemEnabled;
    private float _lastNormalizedTime;

    [SerializeField]
    [Range(0f, 1f)]
    private float _normalizedTime = 0f;

    [SerializeField]
    [Range(0.1f, 3f)]
    private float _playbackSpeed = 1f;

    public Animator CurrentAnimator => _animator;
    public AnimationClip CurrentAnimationClip => _animationClip;
    public float CurrentPlaybackSpeed => _playbackSpeed;

    private void OnValidate()
    {
        if (_animator == null || _animationClip == null || _lastNormalizedTime == _normalizedTime)
            return;

        if(!_systemEnabled)
        {
            AnimationViewer.InitializeSystem(_animationClip, _animator);
            AnimationViewer.OnOperationEnd += OnAnimationViewerOperationEnd;
            _systemEnabled = true;
        }

        _lastNormalizedTime = _normalizedTime;

        AnimationViewer.UpdateAnimation(_normalizedTime);
    }

    private void OnDestroy()
    {
        _systemEnabled = false;
        AnimationViewer.CancelOperation();
    }

    private void OnAnimationViewerOperationEnd()
    {
        _normalizedTime = 0f;
        _lastNormalizedTime = 0f;
        _systemEnabled = false;

        AnimationViewer.OnOperationEnd -= OnAnimationViewerOperationEnd;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(AnimationViewerDemo))]
public class AnimationViewerDemoEditor : Editor
{
    private AnimationViewerDemo _animationViewerDemo;

    private void OnEnable()
    {
        _animationViewerDemo = (AnimationViewerDemo)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Play Animation"))
            AnimationViewer.PlayAnimation(_animationViewerDemo.CurrentAnimationClip, _animationViewerDemo.CurrentAnimator, _animationViewerDemo.CurrentPlaybackSpeed);

        if (GUILayout.Button("Cancel Operation"))
            AnimationViewer.CancelOperation();
    }
}

#endif
