using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Builder
{
    public abstract class AErrorTreeViewItem
    {
        public string Name { get; }

        public AErrorTreeViewItem(string name)
        {
            Name = name;
        }
    }

    public abstract class AErrorTreeViewItemChildren : AErrorTreeViewItem
    {
        public ObservableCollection<AErrorTreeViewItem> Children { get; }

        public AErrorTreeViewItemChildren(string name) : base(name)
        {
            Children = new ObservableCollection<AErrorTreeViewItem>();
        }
    }

    public class ErrorTreeViewItemLog : AErrorTreeViewItemChildren
    {
        public ErrorTreeViewItemLog() : base(string.Empty)
        {
        }
    }

    public class ErrorTreeViewItemCategory : AErrorTreeViewItemChildren
    {
        public ImageSource Icon { get; }

        public ErrorTreeViewItemCategory(ImageSource icon, string name) : base(name)
        {
            Icon = icon;
        }
    }

    public class ErrorTreeViewItemError : AErrorTreeViewItemChildren
    {
        public double Y { get; set; }

        public ErrorTreeViewItemError(string name) : base(name)
        {
        }
    }

    public class ErrorTreeViewItemErrorStackTrace : AErrorTreeViewItem
    {
        public ErrorTreeViewItemErrorStackTrace(string name) : base(name)
        {
        }
    }
}
