using System;
using Unity.VisualScripting;
using UnityEngine;

public class AudioManager : MonoBehaviourSingleton<AudioManager>
{
    AudioSource audioSource;
    [SerializeField] AudioClip acNextSound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayClip(AudioClip aud)
    {
        if (audioSource.isPlaying)
            audioSource.Stop();

        audioSource.PlayOneShot(aud);
    }

    public bool IsPlaying()
    {
        return audioSource.isPlaying;
    }

    public void StopAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    public void PlayNext()
    {
        if (acNextSound != null)
            PlayClip(acNextSound);
    }

    public void PlayTutorial(string auoName)
    {
        try
        {
            PlayClip((AudioClip)Resources.Load("Audios/" + auoName));
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message + ":" + auoName);
        }
    }

}
