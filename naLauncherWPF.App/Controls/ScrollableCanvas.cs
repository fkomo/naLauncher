using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace naLauncherWPF.App.Controls
{
    public class ScrollableCanvasControl : Canvas
    {
        static ScrollableCanvasControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ScrollableCanvasControl), new FrameworkPropertyMetadata(typeof(ScrollableCanvasControl)));
        }

        protected override Size MeasureOverride(Size constraint)
        {
            double bottomMost = 0d;
            double rightMost = 0d;

            foreach (object obj in Children)
            {
                FrameworkElement child = obj as FrameworkElement;

                if (child != null)
                {
                    child.Measure(constraint);

                    bottomMost = Math.Max(bottomMost, GetTop(child) + child.DesiredSize.Height);
                    rightMost = Math.Max(rightMost, GetLeft(child) + child.DesiredSize.Width);
                }
            }

            return new Size(rightMost, bottomMost);
        }
    }
}
