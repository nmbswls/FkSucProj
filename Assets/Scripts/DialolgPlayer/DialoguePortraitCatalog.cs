using System.IO;

namespace My.Dialog
{
    // 对话立绘资源统一目录与 Speaker 映射
    public static class DialoguePortraitCatalog
    {
        public const string PortraitFolder = "Assets/Resources/Arts/Portrait";
        public const string LilithDefault = PortraitFolder + "/lilith_default.png";
        public const string YingyuDefault = PortraitFolder + "/yingyu_default.png";

        public static string ResolveSpeaker(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return null;

            string key = Path.GetFileNameWithoutExtension(imagePath).ToLowerInvariant();
            switch (key)
            {
                case "lilith_default":
                case "lilith":
                    return "莉莉丝";
                case "yingyu_default":
                case "yingyu":
                    return "影羽";
                default:
                    return null;
            }
        }
    }
}
