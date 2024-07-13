using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffectMazeHandler : MonoBehaviour
{
    [Header("Transforms")]
    [SerializeField] private Transform pacmanTransform;
    [SerializeField] private Transform blinkyTransform;
    [SerializeField] private Transform clydeTransform;
    [SerializeField] private Transform inkyTransform;
    [SerializeField] private Transform pinkyTransform;
    
    private Dictionary<string, Transform> transforms = new Dictionary<string, Transform>();

    private void Start()
    {
        transforms["pacman"] = pacmanTransform;
        transforms["blinky"] = blinkyTransform;
        transforms["clyde"] = clydeTransform;
        transforms["inky"] = inkyTransform;
        transforms["pinky"] = pinkyTransform;
    }
    
    public void SpawnStartParticle(GameObject particlePrefab, Sprite particleSprite, string particleName, string character)
    {
        GameObject particleInstance = Instantiate(particlePrefab, transforms[character].position, Quaternion.identity);
        particleInstance.name = particleName;
        particleInstance.transform.SetParent(transforms[character]);

        ParticleSystem particleSystem = particleInstance.GetComponent<ParticleSystem>();

        var textureSheetAnimation = particleSystem.textureSheetAnimation;
        textureSheetAnimation.enabled = true;
        textureSheetAnimation.mode = ParticleSystemAnimationMode.Sprites;
        textureSheetAnimation.AddSprite(particleSprite);

        particleSystem.Play();

        StartCoroutine(FollowTarget(particleInstance, transforms[character], particleSystem.main.duration + particleSystem.main.startLifetime.constantMax));
    }

    private IEnumerator FollowTarget(GameObject particleInstance, Transform followTransform, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (followTransform != null)
            {
                particleInstance.transform.position = followTransform.position;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(particleInstance);
    }
}
