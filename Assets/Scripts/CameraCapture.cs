using UnityEngine;
using System.IO;

public class CameraCapture : MonoBehaviour
{
    public Camera robotCamera;

    public int width = 640;
    public int height = 480;

    void Awake()
    {
        robotCamera = GameObject.Find("Camera").GetComponent<Camera>();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Captured!!");
            Capture();
        }
    }


    void Capture()
    {
        RenderTexture rt = new RenderTexture(
                width,
                height,
                24);

        robotCamera.targetTexture = rt;

        Texture2D image = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false);

        RenderTexture.active = rt;

        robotCamera.Render();

        image.ReadPixels(
            new Rect(0, 0, width, height),
            0,
            0);


        image.Apply();


        byte[] bytes = image.EncodeToPNG();

        string path = Application.dataPath + "/CameraCapture.png";

        File.WriteAllBytes(path, bytes);

        Debug.Log("Saved: " + path);

        robotCamera.targetTexture = null;
        RenderTexture.active = null;

        Destroy(rt);
        Destroy(image);
    }
}
