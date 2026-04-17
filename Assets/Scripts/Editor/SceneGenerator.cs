using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CardBattle.Editor
{
    public static class SceneGenerator
    {
        [MenuItem("CardBattle/Generate Battle Scene")]
        public static void GenerateBattleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Camera setup (45° top-down)
            var cam = Camera.main;
            cam.transform.position = new Vector3(0, 8f, -5f);
            cam.transform.rotation = Quaternion.Euler(55f, 0, 0);
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.12f);
            cam.orthographic = false;
            cam.fieldOfView = 45f;

            // Directional Light
            var light = GameObject.Find("Directional Light");
            if (light) {
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0);
                light.GetComponent<Light>().intensity = 1.2f;
            }

            // Battle Field (plane)
            var field = GameObject.CreatePrimitive(PrimitiveType.Plane);
            field.name = "BattleField";
            field.transform.position = Vector3.zero;
            field.transform.localScale = new Vector3(1.6f, 1, 1);
            var fieldMat = new Material(Shader.Find("Standard"));
            fieldMat.color = new Color(0.1f, 0.12f, 0.18f);
            field.GetComponent<Renderer>().material = fieldMat;

            // Card zones - Player Monster (5 slots)
            CreateCardZones("PlayerMonsterZone", new Vector3(-2.4f, 0.01f, -1f), 5, Color.green);
            // Card zones - Player Spell/Trap (5 slots)
            CreateCardZones("PlayerSpellZone", new Vector3(-2.4f, 0.01f, -2.2f), 5, Color.cyan);
            // Card zones - Opponent Monster (5 slots)
            CreateCardZones("OpponentMonsterZone", new Vector3(-2.4f, 0.01f, 1f), 5, Color.red);
            // Card zones - Opponent Spell/Trap (5 slots)
            CreateCardZones("OpponentSpellZone", new Vector3(-2.4f, 0.01f, 2.2f), 5, Color.magenta);

            // Game Manager
            var gm = new GameObject("GameManager");
            gm.AddComponent<CardBattle.Game.GameManager>();

            // Canvas for UI
            var canvas = new GameObject("Canvas");
            var c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // EventSystem
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Save scene
            string path = "Assets/Scenes/BattleScene.unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"BattleScene created at {path}");
            EditorUtility.DisplayDialog("Scene Generated", "BattleScene.unity has been created in Assets/Scenes/", "OK");
        }

        static void CreateCardZones(string parentName, Vector3 startPos, int count, Color color)
        {
            var parent = new GameObject(parentName);
            parent.transform.position = startPos;
            for (int i = 0; i < count; i++)
            {
                var slot = GameObject.CreatePrimitive(PrimitiveType.Quad);
                slot.name = $"Slot_{i}";
                slot.transform.SetParent(parent.transform);
                slot.transform.localPosition = new Vector3(i * 1.2f, 0, 0);
                slot.transform.rotation = Quaternion.Euler(90, 0, 0);
                slot.transform.localScale = new Vector3(0.7f, 1f, 1f);
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(color.r, color.g, color.b, 0.3f);
                mat.SetFloat("_Mode", 3); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
                slot.GetComponent<Renderer>().material = mat;
            }
        }
    }
}
