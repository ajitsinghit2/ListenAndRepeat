// This file has been autogenerated from parsing an Objective-C header file added in Xcode.

using System;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using ListenAndRepeat.ViewModel;
using ListenAndRepeat.Util;

namespace ListenAndRepeat
{
	public partial class WordListController : UITableViewController
	{
		public WordListController(IntPtr handle) : base (handle)
		{
			mMainModel = ServiceContainer.Resolve<MainModel>();
		}
				
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
								
			var tableSource = new TableSource();
			TableView.Source = tableSource;

			AddButton.Clicked += delegate 
			{
				var addWord = Storyboard.InstantiateViewController("AddWordController") as AddWordController;
				NavigationController.PushViewController(addWord, true);
			};

			mMainModel.WordsListChanged += delegate 
			{
				this.InvokeOnMainThread(delegate
				{
					TableView.ReloadData();
				});
			};


			SetUpEditButton();
		}

		private void SetUpEditButton()
		{						
			mDoneButton = new UIBarButtonItem(UIBarButtonSystemItem.Done, (s,e) =>
			{
				TableView.SetEditing (false, true);
				NavigationItem.RightBarButtonItem = mEditButton;
			});

			mEditButton = new UIBarButtonItem(UIBarButtonSystemItem.Edit, (s,e) =>
			{
				if (TableView.Editing)
					TableView.SetEditing (false, true); // if we've half-swiped a row

				TableView.SetEditing (true, true);
				NavigationItem.RightBarButtonItem = mDoneButton;
			});

			NavigationItem.RightBarButtonItem = mEditButton;
		}
	
		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
		}

		MainModel mMainModel;
		DictionarySearchModel mDictionarySearchModel;
		UIBarButtonItem mEditButton;
		UIBarButtonItem mDoneButton;
	}

	public class TableSource : UITableViewSource 
	{
		static string mCellIdentifier = "WordCell";
		MainModel mMainModel;
		PlaySoundModel mPlaySoundModel;
		
		public TableSource()
		{
			mMainModel = ServiceContainer.Resolve<MainModel>();
			mPlaySoundModel = ServiceContainer.Resolve<PlaySoundModel>();
		}
		
		public override int RowsInSection(UITableView tableview, int section)
		{
			return mMainModel.WordsList.Count;
		}

		public override void AccessoryButtonTapped(UITableView tableView, NSIndexPath indexPath)
		{
			//new UIAlertView("DetailDisclosureButton Touched", mMainModel.WordsList[indexPath.Row], null, "OK", null).Show();
		}
		
		public override UITableViewCell GetCell(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
		{
			UITableViewCell cell = tableView.DequeueReusableCell(mCellIdentifier);

			WordModel theWordModel = mMainModel.WordsList [indexPath.Row];
			cell.TextLabel.Text = theWordModel.Word;

			if (theWordModel.Status == WordStatus.NOT_STARTED) {
				cell.TextLabel.TextColor = UIColor.LightGray;
				cell.DetailTextLabel.Text = "Not Started";
			} else if (theWordModel.Status == WordStatus.QUERYING_AH) {
				cell.TextLabel.TextColor = UIColor.LightGray;
				cell.DetailTextLabel.Text = "Searching...";
			} else if (theWordModel.Status == WordStatus.NOT_FOUND) {
				cell.TextLabel.TextColor = UIColor.Red;
				cell.DetailTextLabel.Text = "Not Found";
			} else if (theWordModel.Status == WordStatus.NETWORK_ERROR) {
				cell.TextLabel.TextColor = UIColor.Red;
				cell.DetailTextLabel.Text = "Network error";
			} else if (theWordModel.Status == WordStatus.GENERAL_ERROR) {
				cell.TextLabel.TextColor = UIColor.Red;
				cell.DetailTextLabel.Text = "General error";
			} else {
				cell.TextLabel.TextColor = UIColor.Black;
				cell.DetailTextLabel.Text = "Downloaded... TODO";
			}

			return cell;
		}

		public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
		{
			mPlaySoundModel.PlaySound(mMainModel.WordsList[indexPath.Row]);

			// iOS convention is to remove the highlight
			tableView.DeselectRow(indexPath, true);
		}

		public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, MonoTouch.Foundation.NSIndexPath indexPath)
		{
			switch (editingStyle)
			{
			case UITableViewCellEditingStyle.Delete:
				mMainModel.RemoveWordAt(indexPath.Row);		
				break;
			case UITableViewCellEditingStyle.None:
				Console.WriteLine ("CommitEditingStyle:None called");
				break;
			}
		}

		public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
		{
			return true;
		}
		public override bool CanMoveRow (UITableView tableView, NSIndexPath indexPath)
		{
			return true;
		}
		public override UITableViewCellEditingStyle EditingStyleForRow (UITableView tableView, NSIndexPath indexPath)
		{
			return UITableViewCellEditingStyle.Delete; // Don't suppport Insert
		}

		public override void MoveRow (UITableView tableView, NSIndexPath sourceIndexPath, NSIndexPath destinationIndexPath)
		{
			mMainModel.ReorderWords(sourceIndexPath.Row, destinationIndexPath.Row);
		}
	}
}
