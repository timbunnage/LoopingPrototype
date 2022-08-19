using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    AudioSource m_AudioSource;

    bool m_Play;

    bool m_ToggleChange;

    // Start is called before the first frame update
    void Start()
    {
        m_AudioSource = GetComponent<AudioSource>();

        m_Play = true;
        
    }

    // Update is called once per frame
    void Update()
    {

        if (m_Play && m_ToggleChange) {
            m_AudioSource.Play();
        }
        
    }
}
