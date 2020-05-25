using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace naLauncherWPF.App.Extensions
{
	public static class GridExtensions
	{
		public static IList<ColumnDefinition> GetColumnDefinitions(DependencyObject obj)
		{
			return (IList<ColumnDefinition>)obj.GetValue(ColumnDefinitionsProperty);
		}

		public static void SetColumnDefinitions(DependencyObject obj, IList<ColumnDefinition> value)
		{
			obj.SetValue(ColumnDefinitionsProperty, value);
		}

		public static readonly DependencyProperty ColumnDefinitionsProperty =
			DependencyProperty.RegisterAttached("ColumnDefinitions", typeof(IList<ColumnDefinition>), typeof(GridExtensions), new PropertyMetadata(null, OnColumnDefinitionsChanged));

		private static void OnColumnDefinitionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var target = d as Grid;
			if (target != null && e.NewValue is IList<ColumnDefinition>)
			{
				target.ColumnDefinitions.Clear();

				foreach (var columnDefinition in e.NewValue as IList<ColumnDefinition>)
					target.ColumnDefinitions.Add(columnDefinition);
			}
		}

		public static IList<RowDefinition> GetRowDefinitions(DependencyObject obj)
		{
			return (IList<RowDefinition>)obj.GetValue(RowDefinitionsProperty);
		}

		public static void SetRowDefinitions(DependencyObject obj, IList<RowDefinition> value)
		{
			obj.SetValue(RowDefinitionsProperty, value);
		}

		public static readonly DependencyProperty RowDefinitionsProperty =
			DependencyProperty.RegisterAttached("RowDefinitions", typeof(IList<RowDefinition>), typeof(GridExtensions), new PropertyMetadata(null, OnRowDefinitionsChanged));

		private static void OnRowDefinitionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var target = d as Grid;
			if (target != null && e.NewValue is IList<RowDefinition>)
			{
				target.RowDefinitions.Clear();

				foreach (var rowDefinition in e.NewValue as IList<RowDefinition>)
					target.RowDefinitions.Add(rowDefinition);
			}
		}
	}
}
