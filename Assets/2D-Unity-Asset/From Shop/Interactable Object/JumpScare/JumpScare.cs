using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class JumpScare : ObjectEffect
{
    public Sprite jumpScareImage;
    public AudioClip jumpScareSound;
    private AudioSource jumpScareSource;
    private GameObject canvas;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            canvas = new GameObject();
            canvas.AddComponent<RectTransform>();
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
            GameObject imageObject = new GameObject();
            imageObject.transform.SetParent(canvas.transform);
            RectTransform imageTransform = imageObject.AddComponent<RectTransform>();
            imageObject.AddComponent<CanvasRenderer>();
            imageTransform.anchorMin = Vector2.zero;
            imageTransform.anchorMax = Vector2.one;
            imageTransform.offsetMin = Vector2.zero;
            imageTransform.offsetMax = Vector2.zero;
            Image image =  imageObject.AddComponent<Image>();
            image.sprite = jumpScareImage;
            jumpScareSource = imageObject.AddComponent<AudioSource>();
            jumpScareSource.PlayOneShot(jumpScareSound);
            StartCoroutine(CloseJumpscare());
            
        }
    }
    private IEnumerator CloseJumpscare()
    {
        yield return new WaitForSeconds(2);
        Destroy(canvas);
        Destroy(this.gameObject); 
    }

}
