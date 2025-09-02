using UnityEngine;

public class PlaySoundEffect : ObjectEffect
{
    public AudioClip clip;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    public override void ApplyEffect( Player player)
    {
        if (audioSource != null && clip != null && !audioSource.isPlaying)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
