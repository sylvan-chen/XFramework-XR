using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using XGame.Core;

public class UIPanelMainMenu : UIPanelBase
{
    [SerializeField] private Button _startGameBtn;
    [SerializeField] private Button _exitGameBtn;

    protected override void OnInit()
    {
        base.OnInit();

        _startGameBtn.onClick.AddListener(OnStartGameClicked);
        _exitGameBtn.onClick.AddListener(OnExitGameClicked);
    }

    private void OnStartGameClicked()
    {
        Log.Info("Start Game button clicked.");
        M.AssetManager.LoadSceneAsync("Test").Forget();
        M.UIManager.ClosePanel(ID);
    }

    private void OnExitGameClicked()
    {
        Log.Info("Exit Game button clicked.");
        M.ShutdownGame();
    }
}