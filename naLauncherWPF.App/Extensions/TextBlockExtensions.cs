using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace naLauncherWPF.App.Extensions
{
	public static class TextBlockExtensions
	{
		public static IList<Inline> GetInlines(DependencyObject obj)
		{
			return (IList<Inline>)obj.GetValue(InlinesProperty);
		}

		public static void SetInlines(DependencyObject obj, IList<Inline> value)
		{
			obj.SetValue(InlinesProperty, value);
		}

		public static readonly DependencyProperty InlinesProperty =
			DependencyProperty.RegisterAttached("Inlines", typeof(IList<Inline>), typeof(TextBlockExtensions), new PropertyMetadata(null, OnInlinesChanged));

		private static void OnInlinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var Target = d as TextBlock;

			if (Target != null)
			{
				Target.Inlines.Clear();
				Target.Inlines.AddRange((System.Collections.IList)e.NewValue);
			}
		}
	}
}
