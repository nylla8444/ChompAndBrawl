using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionHandler : MonoBehaviour
{
    [SerializeField] private GameObject transitionScreen;
    private Animator transitionAnimator;
    
    private void Start()
    {
        // Get the transition data
        string transitionId = TransitionData.transitionId;
        string endTransitionName = TransitionData.endTransitionName;
        
        transitionScreen.SetActive(true);
        try
        {
            transitionAnimator = GameObject.Find(transitionId).GetComponent<Animator>();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("TransitionId: " + transitionId + " does not exist. " + ex.Message);
        }
        
        PlayEndTransition(transitionAnimator, endTransitionName);
    }

    private void PlayEndTransition(Animator _transitionAnimator, string _endTransitionName)
    {
        if (_transitionAnimator != null && !string.IsNullOrEmpty(_endTransitionName))
        {
            _transitionAnimator.SetTrigger(_endTransitionName);
            transitionScreen.SetActive(false);
        }
    }
}
