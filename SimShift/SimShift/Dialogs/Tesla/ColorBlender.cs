using System.Drawing;

namespace SimShift.Dialogs.Tesla
{
    public static class ColorBlender
    {
        public static Color Blend(this Color color, Color backColor, double amount)
        {
            byte r = (byte) ((color.R*amount) + backColor.R*(1 - amount));
            byte g = (byte) ((color.G*amount) + backColor.G*(1 - amount));
            byte b = (byte) ((color.B*amount) + backColor.B*(1 - amount));
            byte a = (byte) color.A;
            return Color.FromArgb(a, r, g, b);
        }
    }
}