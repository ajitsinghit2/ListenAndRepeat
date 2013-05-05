using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using MonoTouch.Foundation;

namespace ListenAndRepeat.ViewModel
{
	public class DictionarySearchModel
	{
		public event EventHandler<SearchCompletedEventArgs> SearchCompleted;
		public event EventHandler IsSearchingChanged;

		public void Search(string word)
		{
			word = Sanitize(word);

			lock (mSearchInProgressSync)
			{
				// Is there another search in progress?
				if (mCurrentThread != null)
					mCurrentThread.Abort();

				mCurrentWord = null;
				mCurrentThread = new Thread(SearchThread);
				mCurrentThread.Start(word);

				IsSearching = true;
			}
		}

		public bool IsSearching
		{
			get { return mIsSearching; }

			protected set
			{
				if (value != mIsSearching)
				{
					mIsSearching = value;

					var method = IsSearchingChanged;
					if (method != null)
						IsSearchingChanged(this, EventArgs.Empty);
				}
			}
		}

		private void SearchThread(object word) 
		{
			try
			{
				string theSearchURL = String.Format("http://ahdictionary.com/word/search.html?q={0}", word);
				string htmlText = new WebClient().DownloadString(new Uri(theSearchURL));
				
				if (!htmlText.Contains("No word definition found"))
				{
					mCurrentWord = new SearchWord((string)word);
					
					ParseWavFiles(htmlText);
					DownloadWavFiles();
				}
			} 
			catch (Exception) {
			}

			OnSearchCompleted();
		}

		protected void OnSearchCompleted()
		{
			var method = SearchCompleted;
			if (method != null)
				method(this, new SearchCompletedEventArgs() { FoundWord = mCurrentWord });

			lock (mSearchInProgressSync)
			{
				mCurrentThread = null;
				IsSearching = false;
			}
		}

		private string Sanitize(string word)
		{
			word = word.Trim();

			// Capitalize
			return word.Substring(0, 1).ToUpper() + word.Substring(1);
		}

		private void DownloadWavFiles()
		{
			Directory.CreateDirectory(MainModel.GetSoundsDirectory());

			foreach (var wave in mCurrentWord.Waves)
			{
				var theDataBytes = new WebClient().DownloadData("http://ahdictionary.com" + wave.Item1);

				// The Library/Cache/ folder might be cleaned out, so it's better to store the files in our
				// own folder and tell the OS to not backup them
				string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				string finalPath = Path.Combine(documentsPath, "..", "Library", "Sounds", wave.Item2);
				File.WriteAllBytes(finalPath, theDataBytes);
				NSFileManager.SetSkipBackupAttribute(finalPath, true);
			}
		}

		private void ParseWavFiles(string text)
		{
			// /application/resources/wavs/T0172200.wav
			Match match = Regex.Match(text, @"/application/resources/wavs/(.*)\.wav", RegexOptions.IgnoreCase);

			while (match != null && match.Success)
			{
				mCurrentWord.Waves.Add(Tuple.Create(match.Groups[0].Value, match.Groups[1].Value + ".wav"));
				match = match.NextMatch();
			}
		}

		private Thread mCurrentThread;
		private readonly object mSearchInProgressSync = new object();
		private bool mIsSearching = false;
		private SearchWord mCurrentWord;
	}

	public class SearchCompletedEventArgs : EventArgs
	{
		public SearchWord FoundWord { get; set; }
	}
	
	public class SearchWord
	{
		public string Word { get { return mWord; } }
		public List<Tuple<string, string>> Waves { get { return mWaves; } }
		
		public SearchWord(string word)
		{
			mWord = word;
			mWaves = new List<Tuple<string, string>>();
		}
		
		private string mWord;
		private List<Tuple<string, string>> mWaves;
	}
}
