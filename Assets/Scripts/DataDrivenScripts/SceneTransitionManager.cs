using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] private TransitionDictionary transitions;
    private string startTransitionName;
    private string endTransitionName;
    private int transitionDuration;
    private Animator transitionAnimator;

    public void LoadScene(string sceneName, string transitionId)
    {
        if (!transitions.transitionDictionary.ContainsKey(transitionId))
        {
            Debug.LogError($"Transition Id {transitionId} does not exist.");
            return;
        }

        // Get transition info from transition dictionary based on transition id
        transitions.transitionDictionary.TryGetValue(transitionId, out TransitionDictionary.TransitionInfo transitionInfo);
        startTransitionName = transitionInfo.startTransitionName;
        endTransitionName = transitionInfo.endTransitionName;
        transitionDuration = transitionInfo.transitionDuration;
        
        // Set the transition data
        TransitionData.transitionId = transitionId;
        TransitionData.endTransitionName = endTransitionName;
        
        transitionAnimator = GameObject.Find(transitionId).GetComponent<Animator>();
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (!string.IsNullOrEmpty(startTransitionName))
        {
            transitionAnimator.SetTrigger(startTransitionName);
            yield return new WaitForSeconds(transitionDuration);
        }

        // Load asynchronously to the selected scene, with loading screen active
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        // Increase the loading progress if the async operation is not yet done
        float progress = 0;
        while (!operation.isDone)
        {
            progress = Mathf.MoveTowards(progress, Mathf.Clamp01(operation.progress / 0.9f), Time.deltaTime);
            
            // Go to the prompt scene if the progress reaches 100%
            if (progress >= 1f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}