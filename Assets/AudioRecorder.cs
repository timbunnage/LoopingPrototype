using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioRecorder : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

        foreach (var device in Microphone.devices)
        {
            Debug.Log("Name: " + device);
        }

        AudioSource audioSource = GetComponent<AudioSource>();
        Debug.Log("MICROPHONE START RECORDING");
        audioSource.clip = Microphone.Start("麦克风阵列 (Realtek(R) Audio)", true, 10, 44100);
        Debug.Log("MICROPHONE FINISHED RECORDING, START PLAYBACK");
        audioSource.Play();
        Debug.Log("MICROPHONE FINISHED PLAYBACK");

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
