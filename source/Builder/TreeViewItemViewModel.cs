using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Media;

namespace Builder
{
    public abstract class ATreeViewItemViewModel
    {
        public ATreeViewItemViewModel Parent { get; }
        public ObservableCollection<ATreeViewItemViewModel> Children { get; }
        public abstract string Name { get; }

        protected ATreeViewItemViewModel()
        {
            Children = new ObservableCollection<ATreeViewItemViewModel>();
        }

        protected ATreeViewItemViewModel(ATreeViewItemViewModel parent)
        {
            Parent = parent;
            Children = new ObservableCollection<ATreeViewItemViewModel>();
        }
    }

    public class TreeViewItemLogViewModel : ATreeViewItemViewModel
    {
        private readonly ErrorTreeViewItemLog _log;

        public override string Name => _log.Name;

        public TreeViewItemLogViewModel(ErrorTreeViewItemLog log) : base(null)
        {
            _log = log;
            foreach (AErrorTreeViewItem item in _log.Children)
            {
                Children.Add(new TreeViewItemCategoryViewModel(item as ErrorTreeViewItemCategory, this));
            }
        }
    }

    public class TreeViewItemMessageViewModel : ATreeViewItemViewModel
    {
        private readonly AErrorTreeViewItem _message;

        public override string Name => _message.Name;

        public TreeViewItemMessageViewModel(AErrorTreeViewItem errorStackTrace, ATreeViewItemViewModel parent) : base(parent)
        {
            _message = errorStackTrace;
        }
    }

    public class TreeViewItemErrorViewModel : ATreeViewItemViewModel
    {
        private readonly ErrorTreeViewItemError _error;

        public override string Name => _error.Name;
        public double Y => _error.Y;

        public TreeViewItemErrorViewModel(ErrorTreeViewItemError error, ATreeViewItemViewModel parent) : base(parent)
        {
            _error = error;
        }
    }

    public class TreeViewItemErrorStackTraceViewModel : ATreeViewItemViewModel
    {
        private readonly ErrorTreeViewItemErrorStackTrace _errorStackTrace;

        public override string Name => _errorStackTrace.Name;

        public TreeViewItemErrorStackTraceViewModel(ErrorTreeViewItemErrorStackTrace errorStackTrace, ATreeViewItemViewModel parent) : base(parent)
        {
            _errorStackTrace = errorStackTrace;
        }
    }

    public class TreeViewItemCategoryViewModel : ATreeViewItemViewModel
    {
        private readonly ErrorTreeViewItemCategory _category;

        public override string Name => _category.Name;
        public ImageSource Icon => _category.Icon;

        public TreeViewItemCategoryViewModel(ErrorTreeViewItemCategory category, ATreeViewItemViewModel parent) : base(parent)
        {
            _category = category;
            _category.Children.CollectionChanged += LogCollectionChanged;
        }

        private void LogCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (object item in args.NewItems)
                    {
                        if (item is ErrorTreeViewItemError error)
                        {
                            TreeViewItemErrorViewModel newItem = new TreeViewItemErrorViewModel(error, this);
                            if (error.Children.Count > 0)
                            {
                                newItem.Children.Add(new TreeViewItemErrorStackTraceViewModel(error.Children[0] as ErrorTreeViewItemErrorStackTrace, newItem));
                            }
                            Children.Add(newItem);
                        }
                        else if (item is AErrorTreeViewItem)
                        {
                            Children.Add(new TreeViewItemMessageViewModel(item as AErrorTreeViewItem, this));
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (object item in args.OldItems)
                    {
                        if (item is AErrorTreeViewItem error)
                        {
                            ATreeViewItemViewModel itemToRemove = Children.Where(x => x.Name == error.Name).First();
                            Children.Remove(itemToRemove);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Children.Clear();
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
