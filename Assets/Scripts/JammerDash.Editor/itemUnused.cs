using JammerDash.Tech;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JammerDash.Editor.Basics
{

    public class itemUnused : MonoBehaviour
    {
        public bool IsLongCube;
        public float longCubeLength;
        void Start()
        {
            if (gameObject.name.Contains("hitter02"))
            {
                IsLongCube = true;
                longCubeLength = GetComponent<SpriteRenderer>().size.x;
            }
            else
            {
                IsLongCube = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (SceneManager.GetActiveScene().name == "LevelDefault")
            {

                if (gameObject.name.Contains("hitter") && LevelDataManager.Instance.cubesize != 0)
                {
                    transform.localScale = new Vector2(LevelDataManager.Instance.cubesize, LevelDataManager.Instance.cubesize);
                }
                else if (LevelDataManager.Instance.cubesize == 0)
                {
                    transform.localScale = new Vector2(CustomLevelDataManager.Instance.cubesize, CustomLevelDataManager.Instance.cubesize);
                }
            }
            else if (SceneManager.GetActiveScene().name == "SampleScene" && Input.GetMouseButtonUp(0) )
            {
                if (transform.localScale != new Vector3(FindObjectOfType<EditorManager>().size.value, FindObjectOfType<EditorManager>().size.value, 0))
                transform.localScale = new Vector3(FindObjectOfType<EditorManager>().size.value, FindObjectOfType<EditorManager>().size.value, 0);
            }
        }
    }

}