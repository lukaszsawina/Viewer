using Stylet;

namespace Viewer.ViewModel
{
    public class ShellViewModel : Screen
    {
        public  CameraImageViewModel CameraImageView { get; private set; }
        public MenuViewModel MenuView { get; private set; }

        public ShellViewModel(CameraImageViewModel cameraImageView, MenuViewModel menuView)
        {
            CameraImageView = cameraImageView;
            MenuView = menuView;
        }

        protected override void OnClose()
        {
            CameraImageView.StopCamera();
            MenuView.CloseWorkspaceProcessing();
            base.OnClose();
        }
    }
}