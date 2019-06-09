using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SpaceInvaders2;
using SQLite;
using Xamarin.Forms;

namespace SpaceInvaders2
{
	public class HighScoresPage : ContentPage
	{
		private ListView listView;
		string DbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "MyDB.db3");

		public HighScoresPage ()
		{
			listView = new ListView
			{
				ItemTemplate = new DataTemplate(() =>
				{
					Label score = new Label();
					score.SetBinding(Label.TextProperty, "Name");
					Label name = new Label();
					name.SetBinding(Label.TextProperty, "Score");

					return new ViewCell
					{
						View = new StackLayout
						{
							Orientation = StackOrientation.Horizontal,
							Children =
									{
										new StackLayout
										{
											VerticalOptions = LayoutOptions.Center,
											Children =
													{
														score,
														name
													}
										}
									}
						}
					};
				})
			};
			
			this.Title = "High Scores";
			var db = new SQLiteConnection(DbPath);
			StackLayout stackLayout = new StackLayout();

			listView.ItemsSource = db.Table<HighScores>().OrderByDescending(x => x.Score).ToList();
			stackLayout.Children.Add(listView);

			Content = stackLayout;
		}
	}
}