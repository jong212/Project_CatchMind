using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class LoginPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] internal InputField Input_NetworkAdress;
    [SerializeField] internal InputField Input_UserName;

    [SerializeField] internal Button Btn_StartAsHostServer;
    [SerializeField] internal Button Btn_StartAsClient;

    [SerializeField] internal Text Text_Error;

    [SerializeField] NetworkingManager _netManager;

    public static LoginPopup Instance { get; private set; }

    private string _originNetworkAddress;

    private void Awake()
    {
        Instance = this;
        Text_Error.gameObject.SetActive(false);
    }

    private void Start()
    {
        SetDefaultNetworkAddress();
    }

    private void OnEnable()
    {
        Input_UserName.onValueChanged.AddListener(OnValueChanged_ToggleButton);
    }

    private void OnDisable()
    {
        Input_UserName.onValueChanged.RemoveListener(OnValueChanged_ToggleButton);
    }

    private void Update()
    {
        CheckNatworkAddressValidOnUpdate();
    }

    private void SetDefaultNetworkAddress()
    {
        // 네트워크 주소 없는 경우, 디폴트 세팅
        if (string.IsNullOrWhiteSpace(NetworkManager.singleton.networkAddress))
        {
            NetworkManager.singleton.networkAddress = "localhost";
        }

        // 네트워크 주소 공란으로 변경될 경우를 대비해 기존 네트워크 주소 보관
        _originNetworkAddress = NetworkManager.singleton.networkAddress;
    }

    private void CheckNatworkAddressValidOnUpdate()
    {
        if (string.IsNullOrWhiteSpace(NetworkManager.singleton.networkAddress))
        {
            NetworkManager.singleton.networkAddress = _originNetworkAddress;
        }

        if(Input_NetworkAdress.text != NetworkManager.singleton.networkAddress)
        {
            Input_NetworkAdress.text = NetworkManager.singleton.networkAddress;
        }
    }

    public void SetUIOnClientDisconnected()
    {
        this.gameObject.SetActive(true);
        Input_UserName.text = string.Empty;
        Input_UserName.ActivateInputField();
    }

    public void SetUIOnAuthValueChanged()
    {
        Text_Error.text = string.Empty;
        Text_Error.gameObject.SetActive(false);
    }

    public void SetUIOnAuthError(string msg)
    {
        Text_Error.text = msg;
        Text_Error.gameObject.SetActive(true);
    }

    public void OnValueChanged_ToggleButton(string userName)
    {
        bool isUserNameValid = !string.IsNullOrWhiteSpace(userName);
        Btn_StartAsHostServer.interactable = isUserNameValid;
        Btn_StartAsClient.interactable = isUserNameValid;
    }

    public void OnClick_StartAsHost()
    {
        if(_netManager == null)
            return;

        _netManager.StartHost();
        this.gameObject.SetActive(false);
    }

    public void OnClick_StartAsClient()
    {
        if (_netManager == null)
            return;

        _netManager.StartClient();
        this.gameObject.SetActive(false);
    }
}
