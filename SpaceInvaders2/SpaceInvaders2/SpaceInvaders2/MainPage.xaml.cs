using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Threading;
using SkiaSharp;
//using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.Forms;
using System.IO;
using SQLite;

namespace SpaceInvaders2
{
    public partial class MainPage : ContentPage
    {
        //LocalApplicationdata because of the sandboxed nature of UWP
        string DbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "MyDBb.db3");

        Ship Ship = new Ship();

        List<Bullet> PlayerBullet = new List<Bullet>();
        List<BasicEnemies> BasicEnemies = new List<BasicEnemies>();
        List<HighScores> HighScores = new List<HighScores>();

        int EnemySpawnY = 50;
        int EnemySpawnX = 0;
        int EnemySize = 50;

        int EnemiesToSpawn = 12;


        int GlobalEnemyXdir = 1;
        float EnemySpeed = 1;
        int CurrentScore;
        int HighScore;

        float ScreenHeight;
        float ScreenWidth;

        #region States
        int CurrentState = MENU;

        const int MENU = 0;
        const int INPLAY = 1;
        const int GAMEOVER = 2;
        const int LEADERBOARD = 3;

        #endregion

        public MainPage()
        {
            InitializeComponent();

            StartLoop();
        }

        public void StartButton()
        {
            GiveShipValues();
            EnemySpawnY = 50;
            EnemySpeed = 1;
            CreateEnemies();
            CurrentState = INPLAY;
            PlayerBullet.Clear();
        }

        public void GiveShipValues()
        {
            Ship.Size = 20;
            Ship.X = (int)Width / 2;
            Ship.Y = (int)Height - Ship.Size * 2;
            Ship.Speed = .3f;
            Ship.TimerMax = 10f;
            Ship.MovementTarget = 0;
        }

        public void CreateEnemies()
        {
            int SpawnMultiplier = 0;
            
            for (int i = 0; i < EnemiesToSpawn; i++)
            {
                EnemySpawnX = (SpawnMultiplier * (int)(EnemySize * 1.1)) + 20;

                BasicEnemies Enemy = new BasicEnemies
                {
                    X = EnemySpawnX,
                    Y = EnemySpawnY,
                    Size = 50,
                };

                SpawnMultiplier++;
                
                if (i == EnemiesToSpawn/2 -1)
                {
                    EnemySpawnY += (int)(Enemy.Size * 1.2);
                    SpawnMultiplier = 0;
                }

                BasicEnemies.Add(Enemy);
            }

        }

        void Update()
        {
            SKCanvas.InvalidateSurface();

            switch (CurrentState)
            {
                case MENU:

                    break;
                case INPLAY:
                    FiringLogic();
                    BulletLogic();
                    CollisionDetection();

                    EnemyLogic();
                    ShipMovement();
                    CheckIfGameOver();
                    break;
            }
        }

        private void CheckIfGameOver()
        {
            if (BasicEnemies.Count == 0)
            {
                EnemySpeed = 1;
                EnemySpawnY = 50;
                CreateEnemies();
                PlayerBullet.Clear();
            }
            int LastIndex = BasicEnemies.Count;

            if (BasicEnemies.Last().Y >= Height - BasicEnemies.Last().Size / 2)
            {
                GameOverLogic();
                CurrentState = GAMEOVER;
                PlayerBullet.Clear();
            }
        }

        private async void GameOverLogic()
        {
            //Defines database
            if (CurrentScore > HighScore)
            {
                var db = new SQLiteConnection(DbPath);

                db.CreateTable<HighScores>();

                var maxPK = db.Table<HighScores>().OrderByDescending(x => x.ID).FirstOrDefault();


                string Input = await InputBox(this.Navigation);

                HighScores highscore = new HighScores()
                {
                    Name = Input,
                    Score = CurrentScore
                };

                HighScore = CurrentScore;

                db.Insert(highscore);

                await Navigation.PushAsync(new HighScoresPage());

            ////save file every time you get a new HighScore
            }
            await Task.Delay(1);

            BasicEnemies.Clear();

            CurrentState = MENU;
            //HighScores.Add(temp);

            
            CurrentScore = 0;
            EnemySpeed = 1;
        }

        //Big Yikes
        public static Task<string> InputBox(INavigation navigation)
        {
            // wait in this proc, until user did his input 
            var tcs = new TaskCompletionSource<string>();

            var lblTitle = new Label { Text = "Title", HorizontalOptions = LayoutOptions.Center, FontAttributes = FontAttributes.Bold };
            var lblMessage = new Label { Text = "Enter new text:" };
            var txtInput = new Entry { Text = "" };

            var btnOk = new Button
            {
                Text = "Ok",
                WidthRequest = 100,
                BackgroundColor = Color.FromRgb(0.8, 0.8, 0.8),
            };
            btnOk.Clicked += async (s, e) =>
            {
                // close page
                var result = txtInput.Text;
                await navigation.PopModalAsync();
                // pass result
                tcs.SetResult(result);
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                WidthRequest = 100,
                BackgroundColor = Color.FromRgb(0.8, 0.8, 0.8)
            };
            btnCancel.Clicked += async (s, e) =>
            {
                // close page
                await navigation.PopModalAsync();
                // pass empty result
                tcs.SetResult(null);
            };

            var slButtons = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { btnOk, btnCancel },
            };

            var layout = new StackLayout
            {
                Padding = new Thickness(0, 40, 0, 0),
                VerticalOptions = LayoutOptions.StartAndExpand,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Orientation = StackOrientation.Vertical,
                Children = { lblTitle, lblMessage, txtInput, slButtons },
            };

            // create and show page
            var page = new ContentPage
            {
                Content = layout
            };
            navigation.PushModalAsync(page);
            // open keyboard
            txtInput.Focus();

            // code is waiting here, until result is passed with tcs.SetResult() in btn-Clicked
            // then proc returns the result
            return tcs.Task;
        }

        private void FiringLogic()
        {
            if (Ship.CanFire == true && Ship.CoolDownTime <= 0)
            {

                BulletConstructor();
                Ship.CoolDownTime = Ship.TimerMax;
            }
            else
            {
                if (Ship.CoolDownTime > 0)
                {
                    Ship.CoolDownTime--;
                }
            }
        }

        private void EnemyLogic()
        {
            //grabs every enemy object in the game
            for (int i = 0; i < BasicEnemies.Count; i++)
            {
                //moves them by their movement direction
                BasicEnemies[i].X += GlobalEnemyXdir * (int)EnemySpeed;

                //checks if an enemies edge is touching the edge of the screen
                if (BasicEnemies[i].X >= Width - BasicEnemies.First().Size || BasicEnemies[i].X <= 0)
                {
                    for (int q = 0; q < BasicEnemies.Count; q++)
                    {
                        BasicEnemies[q].Y += (int)(BasicEnemies[q].Size * 1.2);
                    }
                    //reverses Direction
                    GlobalEnemyXdir *= -1;
                }
            }
            //slowly makes the enemies faster to increase their speed
            EnemySpeed += 0.01f;
        }

        private void CollisionDetection()
        {
            //Grabs every bullet and every enemy in play and runs the is colliding function 
            for (int i = 0; i < PlayerBullet.Count; i++)
            {
                Bullet PlayerBulletTemp = PlayerBullet[i];

                for (int b = 0; b < BasicEnemies.Count; b++)
                {
                    BasicEnemies EnemyTemp = BasicEnemies[b];

                    if (IsColliding(PlayerBulletTemp, EnemyTemp))
                    {
                        EnemySpeed += .1f;
                        CurrentScore++;

                        BasicEnemies.Remove(EnemyTemp);
                        PlayerBullet.Remove(PlayerBulletTemp);
                    }
                }
            }
        }

        // checks if any part of the object are overlapping each other
        public bool IsColliding(Bullet rect1, BasicEnemies rect2)
        {
            if (rect1.X < rect2.X + rect2.Size &&
                rect1.X + rect1.Size > rect2.X &&
                rect1.Y < rect2.Y + rect2.Size &&
                rect1.Y + rect1.Size > rect2.Y)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //moves the ship to its x target, the slower it is the slower it moves, this is to give the illusion of responsiveness
        private void ShipMovement()
        {
            Ship.X += (int)((Ship.MovementTarget - Ship.X) * Ship.Speed);
            Tweening();
        }

        private void Tweening()
        {
            //increases the size of the ship object depending on how far apart it is from its movement target
            Ship.XSize = Math.Abs(Ship.MovementTarget - Ship.X);
            if (Ship.XSize < Ship.Size)
            {
                Ship.XSize = Ship.Size;
            }
            Ship.YSize = Ship.Size;
        }

        //creates bullets and creates them infront of the player object
        private void BulletConstructor()
        {
            Bullet tempBullet = new Bullet
            {
                Size = 10,
                X = Ship.X,
                Y = Ship.Y - (Ship.Size / 2),
                Speed = 5
            };
            PlayerBullet.Add(tempBullet);
        }

        private void BulletLogic()
        {
            //make bullet go up
            for (int i = 0; i < PlayerBullet.Count; i++)
            {
                PlayerBullet[i].Y -= PlayerBullet[i].Speed;
            }
        }

        void Graphics(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas Canvas = e.Surface.Canvas;

            ScreenHeight = e.Info.Height;
            ScreenWidth = e.Info.Width;

            
            Ship.Y = (int)(e.Info.Height - (Ship.Size * 2));

            using (SKPaint g = new SKPaint())
            {
                Canvas.Clear();

                g.Color = Color.Red.ToSKColor();

                Canvas.DrawText($"HighScore: {HighScore}, Score: {CurrentScore}", 0, 20, g);

                Canvas.DrawText($"Time Till Next Shot: {Ship.CoolDownTime}", 0, 40, g);

                if (CurrentState == INPLAY)
                {

                    //Draw here
                    Canvas.DrawRect(Ship.X, Ship.Y, Ship.XSize, Ship.YSize, g);

                    //make list instead of array, as array is static compared to java's non static nature

                    g.Color = Color.Green.ToSKColor();
                    //Paint Bullets
                    for (int i = 0; i < PlayerBullet.Count; i++)
                    {
                        Canvas.DrawRect(PlayerBullet[i].X, PlayerBullet[i].Y, PlayerBullet[i].Size, (int)PlayerBullet[i].Size / 2, g);
                    }

                    g.Color = Color.DarkOrange.ToSKColor();
                    //Paint Enemies
                    for (int i = 0; i < BasicEnemies.Count; i++)
                    {
                        var BE = BasicEnemies;

                        Canvas.DrawRect(BE[i].X, BE[i].Y, BE[i].Size, BE[i].Size, g);
                    }
                }
                else if (CurrentState == MENU)
                {
                    Canvas.DrawRect((float)Width / 2, (float)Height / 2, 100, 100, g);
                    g.Color = Color.AliceBlue.ToSKColor();
                    Canvas.DrawText("Click Me", (float)Width / 2, (float)Height / 2 + 50, g);
                }

            }
        }

        //grabs the input of the user for both android and uwp
        private void SKCanvas_Touch(object sender, SKTouchEventArgs e)
        {
            //e.handled lets the program know to keep allowing the touch, this allows for the user to hold their finger on the screen
            if (e.ActionType == SKTouchAction.Pressed)
            {
                Ship.CanFire = true;
                Ship.MovementTarget = (int)e.Location.X;
                e.Handled = true;
            }
            else if (e.ActionType == SKTouchAction.Released)
            {
                Ship.CanFire = false;
                e.Handled = true;
            }

            if (e.ActionType == SKTouchAction.Moved)
            {
                //Ship.X = (int)e.Location.X;
                Ship.MovementTarget = (int)e.Location.X;
                e.Handled = true;
            }
            if (CurrentState == MENU && e.ActionType == SKTouchAction.Pressed)
            {
                //(float)Width/2,(float)Height/2
                if ((e.Location.X >= Width / 2 - 50 || e.Location.X <= Width / 2 - 50) && (e.Location.Y >= Height / 2 - 50 || e.Location.Y <= Height / 2 - 50))
                {
                    StartButton();
                    //CurrentState = INPLAY;
                }
                e.Handled = true;
            }
        }

        //loops indefinently, this allows the game to update by itself without relying on the xaml to tell the program to update.
        public async void StartLoop()
        {
            while (true)
            {
                Update();
                //The delay allows everything else to run including user inputs
                await Task.Delay(13);
            }
        }
    }

    public class Bullet
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Size { get; set; }
        public int Speed { get; set; }
    }

    public class Ship
    {
        public int MovementTarget { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Xdir { get; set; }
        public float Speed { get; set; }
        public int Size { get; set; }
        public int XSize { get; set; }
        public int YSize { get; set; }
        public bool CanFire { get; set; }
        public float CoolDownTime { get; set; }
        public float TimerMax { get; set; }
    }

    public class BasicEnemies
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Xdir { get; set; }
        public int Ydir { get; set; }
        public int Speed { get; set; }
        public int Size { get; set; }
    }

    [System.Serializable]
    public class HighScores
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
    }
}