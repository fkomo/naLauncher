using System;
using System.Windows.Input;

namespace naLauncherWPF.App.Helpers
{
	public class DelegateCommand : ICommand
	{
		private readonly Action action;
		private readonly Func<bool> canExecute;

		public DelegateCommand(Action action, Func<bool> canExecute = null)
		{
			this.action = action;
			this.canExecute = canExecute;
		}

		public void Execute(object parameter)
		{
			action();
		}

		public bool CanExecute(object parameter)
		{
			if (canExecute == null)
				return true;

			return canExecute();
		}

#pragma warning disable 67
		public event EventHandler CanExecuteChanged;
#pragma warning restore 67
	}

	public class DelegateCommandWithInput<TInput> : ICommand
	{
		private readonly Action<TInput> action;
		private readonly Func<bool> canExecute;

		public DelegateCommandWithInput(Action<TInput> action, Func<bool> canExecute = null)
		{
			this.action = action;
			this.canExecute = canExecute;
		}

		public void Execute(object input)
		{
			action(input != null ? (TInput)input : default(TInput));
		}

		public void Execute(TInput input)
		{
			action(input);
		}

		public bool CanExecute(object parameter)
		{
			if (canExecute == null)
				return true;

			return canExecute();
		}

#pragma warning disable 67
		public event EventHandler CanExecuteChanged;
#pragma warning restore 67
	}
}
