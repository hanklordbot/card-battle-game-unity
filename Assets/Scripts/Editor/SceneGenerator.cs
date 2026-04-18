using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using CardBattle.Game;
using CardBattle.Scene;
using CardBattle.Audio;
using CardBattle.VFX;

namespace CardBattle.Editor
{
    public static class SceneGenerator
    {
        [MenuItem("CardBattle/Generate Battle Scene")]
        public static void GenerateBattleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // === Camera (45° top-down) ===
            var cam = Camera.main;
            cam.transform.position = new Vector3(0, 8f, -5f);
            cam.transform.rotation = Quaternion.Euler(55f, 0, 0);
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.12f);
            cam.fieldOfView = 45f;
            cam.gameObject.AddComponent<CameraController>();

            // === Directional Light ===
            var light = GameObject.Find("Directional Light");
            if (light) { light.transform.rotation = Quaternion.Euler(50f, -30f, 0); light.GetComponent<Light>().intensity = 1.2f; }

            // === Battle Field ===
            var field = GameObject.CreatePrimitive(PrimitiveType.Plane);
            field.name = "BattleField";
            field.transform.localScale = new Vector3(1.6f, 1, 1);
            var fieldMat = new Material(Shader.Find("Standard"));
            fieldMat.color = new Color(0.1f, 0.12f, 0.18f);
            field.GetComponent<Renderer>().material = fieldMat;
            field.AddComponent<BattleFieldSetup>();

            // === Card Zones ===
            CreateCardZones("PlayerMonsterZone", new Vector3(-2.4f, 0.01f, -1f), 5, new Color(0, 1, 0, 0.3f));
            CreateCardZones("PlayerSpellZone", new Vector3(-2.4f, 0.01f, -2.2f), 5, new Color(0, 1, 1, 0.3f));
            CreateCardZones("OpponentMonsterZone", new Vector3(-2.4f, 0.01f, 1f), 5, new Color(1, 0, 0, 0.3f));
            CreateCardZones("OpponentSpellZone", new Vector3(-2.4f, 0.01f, 2.2f), 5, new Color(1, 0, 1, 0.3f));

            // === Game Manager (core game logic) ===
            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();

            // === Audio System ===
            var audio = new GameObject("AudioSystem");
            audio.AddComponent<AudioManager>();
            // BGMController and SFXPool are created dynamically by AudioManager.Awake()

            // === VFX System ===
            var vfx = new GameObject("VFXSystem");
            vfx.AddComponent<SummonVFX>();
            vfx.AddComponent<BattleVFX>();
            vfx.AddComponent<LPVFX>();
            vfx.AddComponent<ResultVFX>();

            // === UI Canvas ===
            var canvas = new GameObject("Canvas");
            var c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvas.AddComponent<GraphicRaycaster>();
            canvas.AddComponent<CardBattle.UI.BattleUI>();

            // --- LP Bars ---
            CreateLPBar(canvas.transform, "Player1LP", new Vector2(200, -40), Color.green);
            CreateLPBar(canvas.transform, "Player2LP", new Vector2(-200, -40), Color.red);

            // --- Buttons ---
            CreateButton(canvas.transform, "EndPhaseBtn", "▶ 下一階段", new Vector2(0, -480), new Vector2(200, 50));
            CreateButton(canvas.transform, "AttackBtn", "⚔ 攻擊", new Vector2(-220, -480), new Vector2(160, 50));
            CreateButton(canvas.transform, "SummonBtn", "✨ 召喚", new Vector2(220, -480), new Vector2(160, 50));
            CreateButton(canvas.transform, "SurrenderBtn", "🏳 投降", new Vector2(400, -480), new Vector2(140, 40));

            // --- Turn / Phase Labels ---
            CreateText(canvas.transform, "TurnLabel", "我方回合", new Vector2(0, 480), 28);
            CreateText(canvas.transform, "PhaseLabel", "DP  SP  MP1  BP  MP2  EP", new Vector2(0, 450), 14);

            // --- Message Panel ---
            var msgPanel = new GameObject("MessagePanel");
            msgPanel.transform.SetParent(canvas.transform, false);
            var msgRt = msgPanel.AddComponent<RectTransform>();
            msgRt.anchoredPosition = Vector2.zero;
            msgRt.sizeDelta = new Vector2(600, 80);
            var msgImg = msgPanel.AddComponent<Image>();
            msgImg.color = new Color(0, 0, 0, 0.7f);
            msgPanel.AddComponent<CanvasGroup>();
            CreateText(msgPanel.transform, "MessageText", "", Vector2.zero, 22);
            msgPanel.SetActive(false);

            // --- Hand Container ---
            var handContainer = new GameObject("HandContainer");
            handContainer.transform.SetParent(canvas.transform, false);
            var handRt = handContainer.AddComponent<RectTransform>();
            handRt.anchorMin = new Vector2(0, 0);
            handRt.anchorMax = new Vector2(1, 0);
            handRt.anchoredPosition = new Vector2(0, 120);
            handRt.sizeDelta = new Vector2(0, 100);

            // --- Game Log ---
            var gameLog = new GameObject("GameLog");
            gameLog.transform.SetParent(canvas.transform, false);
            var logRt = gameLog.AddComponent<RectTransform>();
            logRt.anchorMin = new Vector2(1, 0);
            logRt.anchorMax = new Vector2(1, 0.5f);
            logRt.anchoredPosition = new Vector2(-150, 0);
            logRt.sizeDelta = new Vector2(280, 0);

            // === EventSystem ===
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // === Save ===
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/BattleScene.unity");
            Debug.Log("✅ BattleScene created with all scripts attached!");
            EditorUtility.DisplayDialog("Scene Generated",
                "BattleScene.unity created with:\n" +
                "• GameManager (core logic)\n" +
                "• BattleFieldSetup + CameraController\n" +
                "• AudioManager + BGM + SFX\n" +
                "• VFX (Summon/Battle/LP/Result)\n" +
                "• BattleUI + LP bars + Buttons\n\n" +
                "Press Play to start!", "OK");
        }

        static void CreateCardZones(string name, Vector3 start, int count, Color color)
        {
            var parent = new GameObject(name);
            parent.transform.position = start;
            for (int i = 0; i < count; i++)
            {
                var slot = GameObject.CreatePrimitive(PrimitiveType.Quad);
                slot.name = $"Slot_{i}";
                slot.transform.SetParent(parent.transform);
                slot.transform.localPosition = new Vector3(i * 1.2f, 0, 0);
                slot.transform.rotation = Quaternion.Euler(90, 0, 0);
                slot.transform.localScale = new Vector3(0.7f, 1f, 1f);
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
                slot.GetComponent<Renderer>().material = mat;
            }
        }

        static void CreateLPBar(Transform parent, string name, Vector2 pos, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300, 30);
            var slider = go.AddComponent<Slider>();
            slider.maxValue = 8000;
            slider.value = 8000;

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);
            bg.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 30);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(go.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = color;
            fill.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 30);
            slider.fillRect = fill.GetComponent<RectTransform>();
        }

        static GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.4f, 0.8f);
            go.AddComponent<Button>();
            CreateText(go.transform, "Label", label, Vector2.zero, 18);
            return go;
        }

        static GameObject CreateText(Transform parent, string name, string text, Vector2 pos, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400, 50);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return go;
        }
    }
}
