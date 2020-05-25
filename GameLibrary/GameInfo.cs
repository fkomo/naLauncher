using GameLibrary.GameDataProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace GameLibrary
{
	[DataContract]
	public class GameInfo
	{
		public const string TIMESTAMP_FORMAT = "yyyyMMddHHmmssffff";

		[DataMember]
		public string Title { get; set; }

		[DataMember]
		public string Shortcut { get; set; }

		[DataMember]
		public string TimeStamps { get; set; }

		[DataMember]
		public DateTime Added { get; set; }

		[DataMember]
		public DateTime? Completed { get; set; }

		[DataMember]
		public GameData[] Data { get; set; } = new GameData[] { };

		[DataMember]
		public ConcurrentDictionary<string, DateTime> LastDataUpdate { get; set; } = new ConcurrentDictionary<string, DateTime>();

		public GameInfo()
		{
			
		}

		#region readonly properties

		/// <summary>
		/// total play time in minutes
		/// </summary>
		public int LocalTimePlayed
		{
			get
			{
				var playTime = 0;

				if (TimeStamps?.Length > 0)
				{
					foreach (var timestamp in TimeStamps.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
						if (timestamp.Length > TIMESTAMP_FORMAT.Length)
							playTime += int.Parse(timestamp.Substring(timestamp.IndexOf('+') + 1));
				}

				return playTime;
			}
		}

		public bool Removed { get { return string.IsNullOrEmpty(Shortcut); } }

		public int PlayCount
		{
			get
			{
				return TimeStamps
					?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
					?.Count() ?? 0;
			}
		}

		public int LastMonthPlayCount
		{
			get
			{
				return TimeStamps
					?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
					?.Count(t => DateTime.ParseExact(t.Substring(0, TIMESTAMP_FORMAT.Length), TIMESTAMP_FORMAT, null) > DateTime.Now.AddMonths(-1)) ?? 0;
			}
		}

		public bool? GamepadFriendly
		{
			get
			{
				return (Data
					?.Where(d => d is IControllerSupported && (d as IControllerSupported).GamepadFriendly.HasValue)
					?.OrderByDescending(d => d.Priority)
					?.FirstOrDefault() as IControllerSupported)?.GamepadFriendly;
			}
		}

		public GameImage Image
		{
			get
			{
				return (Data
					?.Where(d => d is IGameImage && (d as IGameImage).Image?.LocalFilename != null/* && File.Exists((d as IGameImage).Image.LocalFilename)*/)
					?.OrderByDescending(d => d.Priority)
					?.FirstOrDefault() as IGameImage)
					?.Image;
			}
		}

		public GameImage[] Images
		{
			get
			{
				return Data
					?.Where(d => d is IGameImage && (d as IGameImage).Image?.LocalFilename != null/* && File.Exists((d as IGameImage).Image.LocalFilename)*/)
					?.OrderByDescending(d => d.Priority)
					?.Select(g => (g as IGameImage).Image)
					?.ToArray();
			}
		}

		public string Summary
		{
			get
			{
				return (Data
					?.Where(d => d is IGameSummary && !string.IsNullOrEmpty((d as IGameSummary).Summary))
					?.OrderByDescending(d => d.Priority)
					?.FirstOrDefault() as IGameSummary)
					?.Summary;
			}
		}

		/// <summary>
		/// average rating from all data providers
		/// </summary>
		public int? Rating
		{
			get
			{
				return (int?)Data
					?.Where(d => d is IRatedGame && (d as IRatedGame).Rating.HasValue)
					?.Select(d => (d as IRatedGame)?.Rating.Value)
					?.Average();
			}
		}

		/// <summary>
		/// Max of PlayTimeForEver from steam games vs local PlayTime (in minutes)
		/// </summary>
		public int TotalTimePlayed
		{
			get
			{
				return Math.Max(CustomData<SteamDbInfoData>()?.PlayTimeForEver ?? 0, LocalTimePlayed);
			}
		}

		/// <summary>
		/// completion time in minutes (sum of play times from timestamps)
		/// </summary>
		public int? BeatenIn
		{
            get
			{
				if (!Completed.HasValue || TimeStamps == null)
					return null;

				var completedIn = 0;
				foreach (var timestamp in TimeStamps.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
					if (timestamp.Length > TIMESTAMP_FORMAT.Length && DateTime.ParseExact(timestamp.Substring(0, TIMESTAMP_FORMAT.Length), TIMESTAMP_FORMAT, null) < Completed.Value)
						completedIn += int.Parse(timestamp.Substring(timestamp.IndexOf('+') + 1));

				return completedIn;
			}
		}

		public DateTime? FirstPlayed
		{
			get
			{
				var first = TimeStamps
					?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
					?.First();

				return first != null ? DateTime.ParseExact(first.Substring(0, TIMESTAMP_FORMAT.Length), TIMESTAMP_FORMAT, null) : null as DateTime?;
			}
		}

		public DateTime? LastPlayed
		{
			get
			{
				var last = TimeStamps
					?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
					?.Last();

				return last != null ? DateTime.ParseExact(last.Substring(0, TIMESTAMP_FORMAT.Length), TIMESTAMP_FORMAT, null) : null as DateTime?;
			}
		}

		public T CustomData<T>() where T : GameData
		{
			return (T)Data?.SingleOrDefault(d => d is T);
		}

		#endregion
	}
}
