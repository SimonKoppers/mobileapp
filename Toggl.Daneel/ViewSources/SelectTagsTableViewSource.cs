using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using MvvmCross.Binding.ExtensionMethods;
using MvvmCross.Binding.iOS.Views;
using Toggl.Daneel.Views;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class SelectTagsTableViewSource : MvxTableViewSource
    {
        private const string cellIdentifier = nameof(SelectableTagViewCell);
        private const string headerCellIdentifier = nameof(SelectTagsHeaderViewCell);

        private IEnumerable<SelectableTagCollection> groupedItems
            => (IEnumerable<SelectableTagCollection>)ItemsSource;
        
        public SelectTagsTableViewSource(UITableView tableView) : base(tableView)
        {
            tableView.RegisterNibForCellReuse(SelectableTagViewCell.Nib, cellIdentifier);
            tableView.RegisterNibForHeaderFooterViewReuse(SelectTagsHeaderViewCell.Nib, headerCellIdentifier);
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            var grouping = getGroupAt(section);
            var cell = getOrCreateHeaderViewFor(tableView);

            if (cell is IMvxBindable bindable)
                bindable.DataContext = grouping;
            
            if (section == 0)
                ((SelectTagsHeaderViewCell)cell).TopSeparatorHidden = true;

            return cell;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var item = GetItemAt(indexPath);

            var cell = GetOrCreateCellFor(tableView, indexPath, item);
            if (cell is SelectableTagViewCell bindable)
                bindable.DataContext = item;
            
            cell.SelectionStyle = UITableViewCellSelectionStyle.None;

            return cell;
        }

        public override nint NumberOfSections(UITableView tableView)
            => ItemsSource.Count();

        public override nint RowsInSection(UITableView tableview, nint section)
            => getGroupAt(section).Count();

        private SelectableTagCollection getGroupAt(nint section)
            => groupedItems.ElementAt((int)section);

        protected override object GetItemAt(NSIndexPath indexPath)
            => groupedItems.ElementAt(indexPath.Section).ElementAt((int)indexPath.Item);

        private UITableViewHeaderFooterView getOrCreateHeaderViewFor(UITableView tableView)
            => tableView.DequeueReusableHeaderFooterView(headerCellIdentifier);
        
        protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item)
            => tableView.DequeueReusableCell(cellIdentifier, indexPath);
        
        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) => 48;

        public override nfloat GetHeightForHeader(UITableView tableView, nint section) => 40;

        public override void HeaderViewDisplayingEnded(UITableView tableView, UIView headerView, nint section)
        {
            var firstVisible = TableView.IndexPathsForVisibleRows.First();
            if (firstVisible.Section != section + 1) return;

            var nextHeader = (SelectTagsHeaderViewCell)TableView.GetHeaderView(firstVisible.Section);
            if (nextHeader == null) return;

            nextHeader.TopSeparatorHidden = true;
        }

        public override void WillDisplayHeaderView(UITableView tableView, UIView headerView, nint section)
        {
            var SelectTagsHeaderViewCell = (SelectTagsHeaderViewCell)headerView;
            var firstVisibleIndexPath = TableView.IndexPathsForVisibleRows.First();
            if (firstVisibleIndexPath.Section == section)
            {
                var nextHeader = (SelectTagsHeaderViewCell)TableView.GetHeaderView(section + 1);
                if (nextHeader == null) return;
                nextHeader.TopSeparatorHidden = false;
                SelectTagsHeaderViewCell.TopSeparatorHidden = true;
            }
            else
            {
                SelectTagsHeaderViewCell.TopSeparatorHidden = false;
            }
        }
    }
}
