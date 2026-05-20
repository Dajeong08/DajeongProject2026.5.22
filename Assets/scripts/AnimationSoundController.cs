using UnityEngine;

public class AnimationSoundController : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        // 오브젝트에 있는 AudioSource 컴포넌트를 가져옵니다.
        audioSource = GetComponent<AudioSource>();
    }

    // 애니메이션 이벤트에서 이 함수를 호출할 겁니다.
    // 파라미터로 오디오 클립(AudioClip)을 직접 받을 수 있습니다.
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}