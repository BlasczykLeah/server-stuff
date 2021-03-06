﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Random = System.Random;
using System.Linq;

[System.Serializable]
public class AudioObject
{
    public AudioCategory type;
    public AudioEffect effect;
    public AudioClip clip;
    public float loudness;
    public bool active;
}

[System.Serializable]
public enum  AudioCategory
{
    Soundtrack, Incidental
}


[System.Serializable]
public enum AudioEffect
{
    CardDraw, CardPlacement, CardPickUp, GoingOut, RoundEnd, YourTurn, LobbySound, GameStart, ButtonPress
}


public class AudioManager : MonoBehaviour
{
    
    public static AudioManager instance;
    
    public AudioSource SoundtrackSource;
    public AudioSource IncidentalSource;

    public AudioObject [] Soundtracks;
    public AudioObject[] Effects;
    public int lastRandomInt = -1;

    public AudioMixer mixer;
    public AudioMixerGroup SoundtrackGroup;
    public AudioMixerGroup FXGroup;

    private void Awake()
    {
        instance = this;
    }

    public void PlayIncidentalSound(AudioObject audioObject)
    {
        IncidentalSource.clip = audioObject.clip;
        IncidentalSource.volume = audioObject.loudness;
        //
        IncidentalSource.PlayOneShot(audioObject.clip, audioObject.loudness);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartSoundtrack()
    {
        StartCoroutine(playSoundTrack());
    }
    
    public void StopSoundtrack()
    {
        StopCoroutine(playSoundTrack());
        SoundtrackSource.Stop();
    }

    public bool IsSoundtrackPlaying()
    {
        return SoundtrackSource.isPlaying;
    }
    
    IEnumerator playSoundTrack()
    {
        if (SoundtrackSource.isPlaying)
            yield return true;
        
        Random r = new Random();
        lastRandomInt = r.Next(0, Soundtracks.Length);
        SoundtrackSource.clip = Soundtracks[lastRandomInt].clip;
        SoundtrackSource.Play();
        
        yield return true;
    }

    public void PlayEffect(AudioEffect e)
    {
        var clip = Array.Find(Effects, element => element.effect == e).clip;
        IncidentalSource.PlayOneShot(clip);
    }
    
    
}
