using UnityEngine;

public class FPD_HeaderAttribute : PropertyAttribute
{
    public string HeaderText;
    public float UpperPadding;
    public float BottomPadding;
    public float Height;

    public FPD_HeaderAttribute(string headerText, float upperPadding = 6f, float bottomPadding = 4f, int addHeight = 2)
    {
        HeaderText = headerText;
        UpperPadding = upperPadding;
        BottomPadding = bottomPadding;
        Height = addHeight;
    }

}

