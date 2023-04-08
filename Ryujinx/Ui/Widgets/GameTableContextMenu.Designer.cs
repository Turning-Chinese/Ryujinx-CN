using Gtk;

namespace Ryujinx.Ui.Widgets
{
    public partial class GameTableContextMenu : Menu
    {
        private MenuItem _openSaveUserDirMenuItem;
        private MenuItem _openSaveDeviceDirMenuItem;
        private MenuItem _openSaveBcatDirMenuItem;
        private MenuItem _manageTitleUpdatesMenuItem;
        private MenuItem _manageDlcMenuItem;
        private MenuItem _manageCheatMenuItem;
        private MenuItem _openTitleModDirMenuItem;
        private MenuItem _openTitleSdModDirMenuItem;
        private Menu     _extractSubMenu;
        private MenuItem _extractMenuItem;
        private MenuItem _extractRomFsMenuItem;
        private MenuItem _extractExeFsMenuItem;
        private MenuItem _extractLogoMenuItem;
        private Menu     _manageSubMenu;
        private MenuItem _manageCacheMenuItem;
        private MenuItem _purgePtcCacheMenuItem;
        private MenuItem _purgeShaderCacheMenuItem;
        private MenuItem _openPtcDirMenuItem;
        private MenuItem _openShaderCacheDirMenuItem;

        private void InitializeComponent()
        {
            //
            // _openSaveUserDirMenuItem
            //
            _openSaveUserDirMenuItem = new MenuItem("打开用户保存目录")
            {
                TooltipText = "打开包含应用程序的用户保存的目录."
            };
            _openSaveUserDirMenuItem.Activated += OpenSaveUserDir_Clicked;

            //
            // _openSaveDeviceDirMenuItem
            //
            _openSaveDeviceDirMenuItem = new MenuItem("打开设备保存目录")
            {
                TooltipText = "打开包含应用程序的设备保存的目录."
            };
            _openSaveDeviceDirMenuItem.Activated += OpenSaveDeviceDir_Clicked;

            //
            // _openSaveBcatDirMenuItem
            //
            _openSaveBcatDirMenuItem = new MenuItem("打开BCAT保存目录")
            {
                TooltipText = "打开包含应用程序的BCAT Saves的目录。"
            };
            _openSaveBcatDirMenuItem.Activated += OpenSaveBcatDir_Clicked;

            //
            // _manageTitleUpdatesMenuItem
            //
            _manageTitleUpdatesMenuItem = new MenuItem("管理title更新")
            {
                TooltipText = "打开“Title更新”管理窗口"
            };
            _manageTitleUpdatesMenuItem.Activated += ManageTitleUpdates_Clicked;

            //
            // _manageDlcMenuItem
            //
            _manageDlcMenuItem = new MenuItem("管理DLC")
            {
                TooltipText = "打开DLC管理窗口"
            };
            _manageDlcMenuItem.Activated += ManageDlc_Clicked;

            //
            // _manageCheatMenuItem
            //
            _manageCheatMenuItem = new MenuItem("管理作弊吗")
            {
                TooltipText = "打开作弊码管理窗口"
            };
            _manageCheatMenuItem.Activated += ManageCheats_Clicked;

            //
            // _openTitleModDirMenuItem
            //
            _openTitleModDirMenuItem = new MenuItem("打开Mods目录")
            {
                TooltipText = "打开包含应用程序模块的目录."
            };
            _openTitleModDirMenuItem.Activated += OpenTitleModDir_Clicked;

            //
            // _openTitleSdModDirMenuItem
            //
            _openTitleSdModDirMenuItem = new MenuItem("大气层模式目录")
            {
                TooltipText = "打开包含应用程序模块的替代SD卡大气层目录."
            };
            _openTitleSdModDirMenuItem.Activated += OpenTitleSdModDir_Clicked;

            //
            // _extractSubMenu
            //
            _extractSubMenu = new Menu();

            //
            // _extractMenuItem
            //
            _extractMenuItem = new MenuItem("提取数据")
            {
                Submenu = _extractSubMenu
            };

            //
            // _extractRomFsMenuItem
            //
            _extractRomFsMenuItem = new MenuItem("RomFS")
            {
                TooltipText = "从应用程序的当前配置中提取RomFS部分（包括更新）."
            };
            _extractRomFsMenuItem.Activated += ExtractRomFs_Clicked;

            //
            // _extractExeFsMenuItem
            //
            _extractExeFsMenuItem = new MenuItem("ExeFS")
            {
                TooltipText = "从应用程序的当前配置中提取ExeFS部分（包括更新）。"
            };
            _extractExeFsMenuItem.Activated += ExtractExeFs_Clicked;

            //
            // _extractLogoMenuItem
            //
            _extractLogoMenuItem = new MenuItem("Logo")
            {
                TooltipText = "从应用程序的当前配置中提取徽标部分（包括更新）。"
            };
            _extractLogoMenuItem.Activated += ExtractLogo_Clicked;

            //
            // _manageSubMenu
            //
            _manageSubMenu = new Menu();

            //
            // _manageCacheMenuItem
            //
            _manageCacheMenuItem = new MenuItem("缓存管理")
            {
                Submenu = _manageSubMenu
            };

            //
            // _purgePtcCacheMenuItem
            //
            _purgePtcCacheMenuItem = new MenuItem("PPTC重建")
            {
                TooltipText = "触发PPTC在下次游戏启动时重建h."
            };
            _purgePtcCacheMenuItem.Activated += PurgePtcCache_Clicked;

            //
            // _purgeShaderCacheMenuItem
            //
            _purgeShaderCacheMenuItem = new MenuItem("清除着色器缓存")
            {
                TooltipText = "删除应用程序的着色器缓存."
            };
            _purgeShaderCacheMenuItem.Activated += PurgeShaderCache_Clicked;

            //
            // _openPtcDirMenuItem
            //
            _openPtcDirMenuItem = new MenuItem("打开PPTC目录")
            {
                TooltipText = "打开包含应用程序PPTC缓存的目录."
            };
            _openPtcDirMenuItem.Activated += OpenPtcDir_Clicked;

            //
            // _openShaderCacheDirMenuItem
            //
            _openShaderCacheDirMenuItem = new MenuItem("打开着色器缓存目录")
            {
                TooltipText = "打开包含应用程序的着色器缓存的目录."
            };
            _openShaderCacheDirMenuItem.Activated += OpenShaderCacheDir_Clicked;

            ShowComponent();
        }

        private void ShowComponent()
        {
            _extractSubMenu.Append(_extractExeFsMenuItem);
            _extractSubMenu.Append(_extractRomFsMenuItem);
            _extractSubMenu.Append(_extractLogoMenuItem);

            _manageSubMenu.Append(_purgePtcCacheMenuItem);
            _manageSubMenu.Append(_purgeShaderCacheMenuItem);
            _manageSubMenu.Append(_openPtcDirMenuItem);
            _manageSubMenu.Append(_openShaderCacheDirMenuItem);

            Add(_openSaveUserDirMenuItem);
            Add(_openSaveDeviceDirMenuItem);
            Add(_openSaveBcatDirMenuItem);
            Add(new SeparatorMenuItem());
            Add(_manageTitleUpdatesMenuItem);
            Add(_manageDlcMenuItem);
            Add(_manageCheatMenuItem);
            Add(_openTitleModDirMenuItem);
            Add(_openTitleSdModDirMenuItem);
            Add(new SeparatorMenuItem());
            Add(_manageCacheMenuItem);
            Add(_extractMenuItem);

            ShowAll();
        }
    }
}