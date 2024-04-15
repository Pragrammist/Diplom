namespace WebHost.Views
{
    public static class ViewsMethodsHelpers
    {
        public static string SetOrGetColor(this LabelInfo label)
        {
            var labelsColors = new Dictionary<string, string>();
            labelsColors["LABEL_1"] = "#23b013";
            labelsColors["LABEL_2"] = "#99eb34";
            labelsColors["LABEL_0"] = "#ebe534";
            labelsColors["LABEL_3"] = "#eb4934";
            labelsColors["LABEL_4"] = "#eb3434";
            return labelsColors[label.LabelName];
        }

        public static string SetOrGetColor(this Lable label)
        {
            var labelsColors = new Dictionary<string, string>();
            labelsColors["LABEL_1"] = "#23b013";
            labelsColors["LABEL_2"] = "#99eb34";
            labelsColors["LABEL_0"] = "#ebe534";
            labelsColors["LABEL_3"] = "#eb4934";
            labelsColors["LABEL_4"] = "#eb3434";
            return labelsColors[label.Label];
        }

        public static string GetLabelClass(string? label)
        {

            return label?.ToLower()?.Replace("_", string.Empty) ?? string.Empty;
        }


        public static string GetLabelClass(this CommentData comment)
        { 
            return GetLabelClass(comment.Label.Label);
        }
    }
}
