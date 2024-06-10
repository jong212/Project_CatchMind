using Mirror.Examples.Chat;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mirror.Examples.MultipleAdditiveScenes
{
    [RequireComponent(typeof(NetworkTransformReliable))]
    public class PlayerController : NetworkBehaviour
    {
        public GameObject newPrefab;
        public GameObject[] Prefabs;
        public RectTransform canvasRectTransform;
        public RectTransform RawRectTransform;
        public TextMeshProUGUI textMeshProUGUI;
        public InputField IField;
        public GameObject cld;

        [SyncVar(hook = nameof(OnPositionChanged))]
        private Vector2 syncedPosition;

        public enum GroundState : byte { Jumping, Falling, Grounded }

        [SyncVar(hook = nameof(OnTextChanged))]
        private string syncedText = "";

        [SyncVar(hook = nameof(OnAlignmentChanged))]
        private TextAlignmentOptions syncedAlignment;

        [Command]
        void CmdUpdatePosition(Vector2 newPosition)
        {
            syncedPosition = newPosition;
        }

        void OnPositionChanged(Vector2 oldPosition, Vector2 newPosition)
        {
            Debug.Log($"OnPositionChanged: oldPosition={oldPosition}, newPosition={newPosition}");
            if (canvasRectTransform != null)
            {
                canvasRectTransform.anchoredPosition = newPosition;
            }
        }

        public override void OnStartAuthority()
        {
            IField = GameObject.FindGameObjectWithTag("field").GetComponent<InputField>();
            Debug.Log($"cld: {cld} ??");
            Debug.Log($"IField: {IField}");
            this.enabled = true;

            if (isLocalPlayer)
            {
                Debug.Log("OnStartAuthority: Initializing components for local player.");
                InitializeComponents();
                string playerId = PlayerPrefs.GetString("PlayerID", "No ID Found");
                if (!string.IsNullOrEmpty(playerId))
                {
                    string characternum = DatabaseUI.Instance.SelectPlayercharacterNumber(playerId);
                    if (!string.IsNullOrEmpty(characternum))
                    {
                        string sceneName = SceneManager.GetActiveScene().name;
                        CmdReplaceChild(sceneName, characternum);
                    }
                }
            }
        }

        void InitializeComponents()
        {
            Canvas canvas = transform.GetChild(0).GetComponent<Canvas>();
            if (canvas != null)
            {
                canvasRectTransform = canvas.GetComponent<RectTransform>();
                RawRectTransform = FindFirstChildRectTransform(canvasRectTransform);
                Debug.Log($"RawRectTransform: {RawRectTransform}");
                textMeshProUGUI = canvas.GetComponentInChildren<TextMeshProUGUI>(true);
                if (textMeshProUGUI == null)
                {
                    Debug.LogError("TextMeshProUGUI component not found during initialization.");
                }
            }
            else
            {
                Debug.LogError("Canvas component not found during initialization.");
            }
        }

        RectTransform FindFirstChildRectTransform(RectTransform parent)
        {
            foreach (Transform child in parent)
            {
                RectTransform rectTransform = child.GetComponent<RectTransform>();
                if (rectTransform != null && rectTransform != canvasRectTransform)
                {
                    return rectTransform;
                }
                RectTransform found = FindFirstChildRectTransform(rectTransform);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        public override void OnStopAuthority()
        {
            this.enabled = false;
        }

        void Update()
        {
            if (isLocalPlayer)
            {
                if (canvasRectTransform == null || textMeshProUGUI == null)
                {
                    InitializeComponents();
                }
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    string inputText = IField.text;
                    CmdChangeText(inputText);
                }
            }
        }

        void OnTextChanged(string oldText, string newText)
        {
            Debug.Log($"OnTextChanged called with text: {newText}");
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.text = newText;
                StartCoroutine(ActivateChildTemporarily(3f)); // Activate for 3 seconds
            }
        }

        private IEnumerator ActivateChildTemporarily(float duration)
        {
            cld.SetActive(true);
            yield return new WaitForSeconds(duration);
            cld.SetActive(false);
        }

        void OnAlignmentChanged(TextAlignmentOptions oldAlignment, TextAlignmentOptions newAlignment)
        {
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.alignment = newAlignment;
            }
        }

        [Command]
        void CmdChangeText(string inputText)
        {
            syncedText = inputText;
        }

        [Command]
        void CmdReplaceChild(string sceneName, string characternum)
        {
            string numberPart = System.Text.RegularExpressions.Regex.Match(characternum, @"\d+").Value;
            Debug.Log($"CmdReplaceChild numberPart: {numberPart}");
            if (int.TryParse(numberPart, out int intnum))
            {
                if (intnum < 0 || intnum >= Prefabs.Length)
                {
                    Debug.LogError("Invalid character index.");
                    return;
                }

                GameObject newChild = Instantiate(Prefabs[intnum], transform.position, Prefabs[intnum].transform.rotation);
                Scene targetScene = SceneManager.GetSceneByName(sceneName);
                SceneManager.MoveGameObjectToScene(newChild, targetScene);
                newChild.transform.SetParent(transform);
                NetworkServer.Spawn(newChild, connectionToClient);
                Debug.Log($"Spawned newChild: {newChild.name}");
                ResetClientToZero(newChild);
            }
            else
            {
                Debug.LogError("CmdReplaceChild: Invalid character number.");
            }
        }

        [ClientRpc]
        void ResetClientToZero(GameObject obj)
        {
            if (obj != null)
            {
                Debug.Log($"ResetClientToZero - Before: {obj.transform.rotation}");
                obj.transform.localPosition = transform.position;
                Debug.Log($"ResetClientToZero - After: {obj.transform.rotation}");
            }
            else
            {
                Debug.LogError("ResetClientToZero: Object is null.");
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isServer)
            {
                InitializeComponents();
                cld = transform.GetChild(0).gameObject;
            }
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.text = syncedText;
                textMeshProUGUI.alignment = syncedAlignment;
            }
        }

        [TargetRpc]
        public void TargetUpdatePlayerPosition(NetworkConnection target, Vector3 position, int playerIndex)
        {
            if (canvasRectTransform == null || textMeshProUGUI == null)
            {
                InitializeComponents(); // Reinitialize components if needed
            }

            if (canvasRectTransform != null)
            {
                transform.position = position;
                Vector2 newPosition;
                if (playerIndex % 2 == 0) // Even index
                {
                    newPosition = new Vector2(3.23f, 1.98f);
                }
                else // Odd index
                {
                    newPosition = new Vector2(-3.55f, 2.03f);
                    Vector3 currentRotation = RawRectTransform.transform.rotation.eulerAngles;
                    currentRotation.y = 180f;
                    RawRectTransform.transform.rotation = Quaternion.Euler(currentRotation);
                }

                Debug.Log($"TargetUpdatePlayerPosition: Setting new position {newPosition} for playerIndex {playerIndex}");
                canvasRectTransform.anchoredPosition = newPosition;
                CmdUpdatePosition(newPosition);

                if (playerIndex % 2 == 0)
                {
                    CmdChangeAlignment(TextAlignmentOptions.Left);
                }
                else
                {
                    CmdChangeAlignment(TextAlignmentOptions.Right);
                }
            }
            else
            {
                Debug.LogError("Canvas RectTransform not found in TargetUpdatePlayerPosition.");
            }
        }

        [Command]
        void CmdChangeAlignment(TextAlignmentOptions alignment)
        {
            syncedAlignment = alignment;
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.alignment = alignment;
            }
        }
    }
}