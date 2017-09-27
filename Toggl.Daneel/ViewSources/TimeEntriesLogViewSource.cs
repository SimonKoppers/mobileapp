using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using Foundation;
using MvvmCross.Binding.ExtensionMethods;
using MvvmCross.Binding.iOS.Views;
using MvvmCross.Core.ViewModels;
using MvvmCross.Plugins.Color.iOS;
using Toggl.Daneel.Views;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class TimeEntriesLogViewSource : MvxTableViewSource
    {
        private const string cellIdentifier = nameof(TimeEntriesLogViewCell);
        private const string headerCellIdentifier = nameof(TimeEntriesLogHeaderViewCell);
        
        private IEnumerable<TimeEntryViewModelCollection> groupedItems
            => ItemsSource as IEnumerable<TimeEntryViewModelCollection>;

        public IMvxAsyncCommand<TimeEntryViewModel> ContinueTimeEntryCommand { get; set; }

        public TimeEntriesLogViewSource(UITableView tableView)
            : base(tableView)
        {
            tableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            tableView.RegisterNibForCellReuse(TimeEntriesLogViewCell.Nib, cellIdentifier);
            tableView.RegisterNibForHeaderFooterViewReuse(TimeEntriesLogHeaderViewCell.Nib, headerCellIdentifier);
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            var grouping = getGroupAt(section);

            var cell = getOrCreateHeaderViewFor(tableView);
            if (cell is IMvxBindable bindable)
                bindable.DataContext = grouping;
            
            return cell;
        }

        public override UIView GetViewForFooter(UITableView tableView, nint section)
        {
            var rect = new CGRect(0, 0, UIScreen.MainScreen.Bounds.Width, 24);
            var footerView = new UIView(rect)
            {
                BackgroundColor = Color.TimeEntriesLog.SectionFooter.ToNativeColor()
            };

            return footerView;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var item = GetItemAt(indexPath);

            var cell = GetOrCreateCellFor(tableView, indexPath, item);
            if (cell is TimeEntriesLogViewCell bindable)
            {
                bindable.DataContext = item;
                bindable.ContinueTimeEntryCommand = ContinueTimeEntryCommand;
            }
            
            cell.SelectionStyle = UITableViewCellSelectionStyle.None;

            return cell;
        }

        public override nint NumberOfSections(UITableView tableView)
            => ItemsSource.Count();

        public override nint RowsInSection(UITableView tableview, nint section)
            => getGroupAt(section).Count();

        private TimeEntryViewModelCollection getGroupAt(nint section)
            => groupedItems.ElementAt((int)section);

        protected override object GetItemAt(NSIndexPath indexPath)
            => groupedItems.ElementAtOrDefault(indexPath.Section)?.ElementAtOrDefault((int)indexPath.Item);

        private UITableViewHeaderFooterView getOrCreateHeaderViewFor(UITableView tableView)
            => tableView.DequeueReusableHeaderFooterView(headerCellIdentifier);

        protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item)
            => tableView.DequeueReusableCell(cellIdentifier, indexPath);

        public override nfloat GetHeightForHeader(UITableView tableView, nint section) => 43;

        public override nfloat GetHeightForFooter(UITableView tableView, nint section) => 24;

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) => 64;

    }
}
