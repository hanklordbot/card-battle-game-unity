using UnityEngine;
using CardBattle.Core;

namespace CardBattle.Scene
{
    public class BattleFieldSetup : MonoBehaviour
    {
        [Header("Field Dimensions")]
        [SerializeField] private float fieldWidth = 20f;
        [SerializeField] private float fieldLength = 14f;
        [SerializeField] private float cardSpacing = 2.5f;

        [Header("Slot References")]
        public Transform[] player1MonsterSlots = new Transform[5];
        public Transform[] player1SpellTrapSlots = new Transform[5];
        public Transform[] player2MonsterSlots = new Transform[5];
        public Transform[] player2SpellTrapSlots = new Transform[5];

        [Header("Materials")]
        [SerializeField] private Material groundMaterial;

        private void Start()
        {
            CreateGround();
            CreateSlots();
            SetupCamera();
        }

        private void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "BattleField_Ground";
            ground.transform.SetParent(transform);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localScale = new Vector3(fieldWidth / 10f, 1f, fieldLength / 10f);

            if (groundMaterial != null)
                ground.GetComponent<Renderer>().material = groundMaterial;
        }

        private void CreateSlots()
        {
            float monsterRowZ_P1 = -1.5f;
            float spellRowZ_P1 = -4f;
            float monsterRowZ_P2 = 1.5f;
            float spellRowZ_P2 = 4f;
            float startX = -(cardSpacing * 2);

            for (int i = 0; i < 5; i++)
            {
                float x = startX + i * cardSpacing;

                player1MonsterSlots[i] = CreateSlot($"P1_Monster_{i}", new Vector3(x, 0.01f, monsterRowZ_P1));
                player1SpellTrapSlots[i] = CreateSlot($"P1_SpellTrap_{i}", new Vector3(x, 0.01f, spellRowZ_P1));
                player2MonsterSlots[i] = CreateSlot($"P2_Monster_{i}", new Vector3(x, 0.01f, monsterRowZ_P2));
                player2SpellTrapSlots[i] = CreateSlot($"P2_SpellTrap_{i}", new Vector3(x, 0.01f, spellRowZ_P2));
            }
        }

        private Transform CreateSlot(string slotName, Vector3 position)
        {
            var slot = new GameObject(slotName);
            slot.transform.SetParent(transform);
            slot.transform.localPosition = position;

            // Visual indicator
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            indicator.name = "SlotIndicator";
            indicator.transform.SetParent(slot.transform);
            indicator.transform.localPosition = Vector3.zero;
            indicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            indicator.transform.localScale = new Vector3(1.5f, 2.1f, 1f);

            var renderer = indicator.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = new Color(1f, 1f, 1f, 0.15f);
            renderer.material.SetFloat("_Mode", 3); // Transparent
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt("_ZWrite", 0);
            renderer.material.DisableKeyword("_ALPHATEST_ON");
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            renderer.material.renderQueue = 3000;

            return slot.transform;
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.transform.position = new Vector3(0f, 12f, -10f);
            cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        }

        public Transform GetMonsterSlot(int player, int index)
        {
            return player == 0 ? player1MonsterSlots[index] : player2MonsterSlots[index];
        }

        public Transform GetSpellTrapSlot(int player, int index)
        {
            return player == 0 ? player1SpellTrapSlots[index] : player2SpellTrapSlots[index];
        }
    }
}
