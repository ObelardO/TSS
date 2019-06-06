// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using UnityEngine;
using UnityEditor;

namespace TSS.Editor
{
    public static class TSSEditorTextures
    {
        #region Properties

        private static Texture2D _TSSIcon;
        public static Texture2D TSSIcon
        {
            get
            {
                if (_TSSIcon == null) _TSSIcon = LoadExternalTexture("TSSIcon");
                return _TSSIcon;
            } 
        }

        private static Texture2D _itemChainIcon;
        public static Texture2D itemChainIcon
        {
            get
            {
                if (_itemChainIcon == null) _itemChainIcon = LoadInternalTexture(@"icons/processed/unityengine/eventsystems/d_eventsystem icon.asset",
                                                                                 @"icons/processed/unityengine/eventsystems/eventsystem icon.asset");
                return _itemChainIcon;
            }
        }

        private static Texture2D _playIcon;
        public static Texture2D playIcon
        {
            get
            {
                if (_playIcon == null) _playIcon = LoadInternalTexture(@"icons/d_playbutton.png", @"icons/playbutton.png");
                return _playIcon;
            }
        }

        private static Texture2D _itemRecordClose;
        public static Texture2D itemRecordClose
        {
            get
            {
                if (_itemRecordClose == null) _itemRecordClose = LoadInternalTexture(@"icons/d_animationkeyframe.png", @"icons/animationkeyframe.png");
                return _itemRecordClose;
            }
        }

        private static Texture2D _itemRecordOpen;
        public static Texture2D itemRecordOpen
        {
            get
            {
                if (_itemRecordOpen == null) _itemRecordOpen = LoadInternalTexture(@"icons/d_animationkeyframe.png", @"icons/animationkeyframe.png");
                return _itemRecordOpen;
            }
        }

        private static Texture2D _itemOpen;
        public static Texture2D itemOpen
        {
            get
            {
                if (_itemOpen == null) _itemOpen = LoadInternalTexture(@"icons/d_animation.nextkey.png", @"icons/animation.nextkey.png");
                return _itemOpen;
            }
        }

        private static Texture2D _itemClose;
        public static Texture2D itemClose
        {
            get
            {
                if (_itemClose == null) _itemClose = LoadInternalTexture(@"icons/d_animation.prevkey.png", @"icons/animation.prevkey.png");
                return _itemClose;
            }
        }

        private static Texture2D _stopIcon;
        public static Texture2D stopIcon
        {
            get
            {
                if (_stopIcon == null) _stopIcon = LoadInternalTexture(@"icons/d_pausebutton.png", @"icons/pausebutton.png");
                return _stopIcon;
            }
        }

        private static Texture2D _handlerIcon;
        public static Texture2D handlerIcon
        {
            get
            {
                if (_handlerIcon == null) _handlerIcon = LoadInternalTexture(@"builtin skins/lightskin/images/tl playhead.png");
                return _handlerIcon;
            }
        }

        private static Texture2D _timeLineItem;
        public static Texture2D timeLineItem
        {
            get
            {
                if (_timeLineItem == null) _timeLineItem = LoadInternalTexture(@"builtin skins/darkskin/images/animationrowoddsemiselected.png",
                                                                               @"builtin skins/lightskin/images/animationrowoddsemiselected.png");
                return _timeLineItem;
            }
        }

        private static Texture2D _timeLineHeader;
        public static Texture2D timeLineHeader
        {
            get
            {
                if (_timeLineHeader == null) _timeLineHeader = LoadInternalTexture(@"builtin skins/darkskin/images/channelstrip_attenuationbar.png",
                                                                                   @"builtin skins/lightskin/images/channelstrip_attenuationbar.png");
                return _timeLineHeader;
            }
        }

        private static Texture2D _timeLineControlLine;
        public static Texture2D timeLineControlLine
        {
            get
            {
                if (_timeLineControlLine == null) _timeLineControlLine = LoadInternalTexture(@"builtin skins/darkskin/images/animationeventbackground.png",
                                                                                             @"builtin skins/lightskin/images/animationeventbackground.png");
                return _timeLineControlLine;
            }
        }

        private static Texture2D _timeLineSelectedItem;
        public static Texture2D timeLineSelectedItem
        {
            get
            {
                if (_timeLineSelectedItem == null) _timeLineSelectedItem = LoadInternalTexture(@"builtin skins/darkskin/images/animationrowevenselected.png",
                                                                                               @"builtin skins/lightskin/images/animationrowevenselected.png");
                return _timeLineSelectedItem;
            }
        }

        private static Texture2D _timeLineChainItem;
        public static Texture2D timeLineChainItem
        {
            get
            {
                if (_timeLineChainItem == null) _timeLineChainItem = LoadInternalTexture(@"builtin skins/darkskin/images/animationrowoddsemiselected.png",
                                                                                         @"builtin skins/lightskin/images/animationrowoddsemiselected.png");
                return _timeLineChainItem;
            }
        }

        #endregion

        #region Loading textures

        private static Texture2D LoadExternalTexture(string fileName)
        {
            return Resources.Load("TSS/Textures/" + fileName) as Texture2D;
        }

        private static Texture2D LoadInternalTexture(string fileName, string lightSkinName = null)
        {
            if (string.IsNullOrEmpty(lightSkinName)) lightSkinName = fileName;

            if (EditorGUIUtility.isProSkin) return EditorGUIUtility.Load(fileName) as Texture2D;
            else return EditorGUIUtility.Load(lightSkinName) as Texture2D;
        }

        #endregion
    }
}
