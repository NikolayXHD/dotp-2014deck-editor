﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Be.Timvw.Framework.ComponentModel;
using RSN.Tools;
using static RSN.DotP.LegalityTools;

namespace RSN.DotP
{
	public class CardInfo
	{
		// This "Casting Cost Cache" should both reduce memory usage and speed up accessing the
		//	CastingCostImage property by allowing any specific casting cost to only be built
		//	once for all cards.  For example once I build {2}{B} I will add a reference to that
		//	image to the cache, then when a card needs an image for {2}{B} I will simply pass
		//	back the reference from the cache.  Since the card will maintain a reference to its
		//	casting cost image once one has been generated it will retain the memory for that
		//	image, this will prevent us from building multiple images for the same casting cost.
		private static Dictionary<string, Bitmap> m_dicCastingCostCache;

		// These are the data fields for a "card" and generally represent the attributes of a
		//	card in Magic as well as the tags in the DotP card XML definition.
		private string m_strActualFilename;
		private string m_strFilename;
		private string m_strCardName;
		private int m_nMultiverseId;
		private Dictionary<string, string> m_dicName;
		private Dictionary<string, string> m_dicFlavourText;
		private string m_strWadName;
		private string m_strImageName;
		private string m_strCardXML;
		private string m_strCastingCost;
		private string m_strColour;
		private int m_nCMC;
		private List<CardSubType> m_lstSubTypes;
		private CardSuperType m_eSuperType;
		private CardType m_eType;
		private ColourFlags m_eColours;
		private CardRarity m_eRarity;
		private List<Ability> m_lstAbilities;
		private string m_strPower;
		private string m_strToughness;
		private string m_strArtist;
		private string m_strExpansion;
		private SortedSet<string> m_setRegisteredTokens;
		private bool m_bToken;
        private bool m_bUnderscore;
		private string m_strFrameType;
        private string m_strFlipFileName;
        private List<Legality> m_legLegalities;

		// These are values that are derived from the card attributes at one point or another.
		//	These may also be additional convenience members so that expensive operations don't
		//	have to keep being done multiple times.
		private bool m_bIsHybrid;
		private bool m_bIsPhyrexian;
		private bool m_bMustBeIncludedInWad;		// Currently unused.
		private Bitmap m_imgCastingCost;
		private GameDirectory m_gdData;
		private Dictionary<string, string> m_dicTypeLine;
		private Dictionary<string, string> m_dicAbilitiesText;
		private Dictionary<string, Bitmap> m_dicCardPreview;

		// For supporting custom Card Tags
		private Dictionary<string, SortableBindingList<string>> m_dicCustomTags;

		// Private variables for drawing preview card
		private Font m_ftCardArtistFont = new Font(FontFamily.GenericSerif, 8, FontStyle.Bold);
		private Font m_ftCardNameFont = new Font(FontFamily.GenericSerif, 14, FontStyle.Bold);
		private Font m_ftCardTypeFont = new Font(FontFamily.GenericSerif, 12);
		private Font m_ftCardTextFontRegular = new Font(FontFamily.GenericSerif, 11);
		private Font m_ftCardTextFontItalic = new Font(FontFamily.GenericSerif, 11, FontStyle.Italic);
		private Brush m_brCardTextBrush = new SolidBrush(Color.Black);
		private Brush m_brCardArtistWhite = new SolidBrush(Color.White);
		private Rectangle m_rcCardImageFull = new Rectangle(0, 0, 356, 512);
		private Rectangle m_rcCardImagePicture = new Rectangle(16, 47, 324, 238);
		private Rectangle m_rcCardNameRegular = new Rectangle(12, 14, 330, 22);
		private Rectangle m_rcCardNameToken = new Rectangle(12, 14, 332, 22);
		private Rectangle m_rcCardPTBox = new Rectangle(245, 453, 130, 65);
		private Rectangle m_rcCardPTBoxAsName = new Rectangle(-50, -8, 460, 65);
		private Rectangle m_rcCardPTBoxText = new Rectangle(280, 476, 60, 20);
		private Rectangle m_rcCardTypeLineRegular = new Rectangle(14, 296, 328, 18);
		private Rectangle m_rcCardTypeLineToken = new Rectangle(14, 349, 328, 18);
		private Rectangle m_rcCardTextRegular = new Rectangle(15, 326, 324, 149);
		private Rectangle m_rcCardTextToken = new Rectangle(16, 385, 324, 85);
		private Rectangle m_rcCardArtistImage = new Rectangle(10, 488, 34, 9);
		private Rectangle m_rcCardExpansionImage = new Rectangle(302, 292, 50, 25);
		private int m_nChaosTextXShift = 25;
		private int m_nChaosTextYShift = 15;
		private int m_nHardReturnBuffer = 4;
		private int m_nManaIconShiftX = 18;
		private int m_nManaIconShiftY = 20;
		private int m_nManaTextShift = 15;
		private int m_nSpaceRemainingBuffer = 40;
		private int m_nTextShiftOffset = -4;
		private Point m_ptArtistCredit = new Point(42, 484);
		private Point m_ptChaosOffset = new Point(2, -3);
		private Point m_ptManaIconOffset = new Point(-7, -7);
		private Point m_ptManaTextOffset = new Point(-4, -6);
		private Point m_ptManaIconStart = new Point(336, 9);
		private Size m_szChaosTarget = new Size(24, 20);
		private Size m_szManaIconTarget = new Size(32, 32);
		private Size m_szManaTextTarget = new Size(26, 26);
		private StringFormat m_sfFullCenter;
		private StringFormat m_sfLeftCenter;
		private StringFormat m_sfCardText;


		public CardInfo(string strFilename, string strXML, string strWad = "", GameDirectory gdData = null)
		{
			m_dicCardPreview = new Dictionary<string, Bitmap>();
			m_dicTypeLine = new Dictionary<string, string>();
			m_dicAbilitiesText = new Dictionary<string, string>();
			m_dicFlavourText = new Dictionary<string, string>();
			m_setRegisteredTokens = new SortedSet<string>();
			m_lstAbilities = new List<Ability>();
			m_lstSubTypes = new List<CardSubType>();
			m_gdData = gdData;
			m_strCardXML = strXML;
			if (strWad.Length > 0)
				m_bMustBeIncludedInWad = false;
			else
				m_bMustBeIncludedInWad = true;
			m_strWadName = strWad;
			m_strPower = null;
			m_strToughness = null;
			m_strCastingCost = null;
			m_imgCastingCost = null;
			m_strActualFilename = Path.GetFileNameWithoutExtension(strFilename);
			m_strFilename = null;		// Will get set properly during ParseXML() if the Card is properly in a CARD_V2 tag.
			m_strCardName = null;
			m_strFrameType = string.Empty;
			m_strColour = null;
			m_dicCustomTags = new Dictionary<string, SortableBindingList<string>>();

			ParseXML(m_strCardXML);
		}

		public string ActualFilename
		{
			get { return m_strActualFilename; }
		}

		public string Filename
		{
			get { return m_strFilename; }
		}

		public string CardName
		{
			get { return m_strCardName; }
		}

		public string LocalizedName
		{
			get
			{
				if (Settings.Language.MasqueradeAsLangCode != null)
					return GetLocalizedName(Settings.Language.MasqueradeAsLangCode);
				else
					return GetLocalizedName(Settings.Language.LanguageCode);
			}
		}

		public Dictionary<string, string> Name
		{
			get { return m_dicName; }
		}

		public string GetLocalizedName(string strLangCode)
		{
			string strReturn = string.Empty;

			if (m_dicName.ContainsKey(strLangCode))
				strReturn = m_dicName[strLangCode];
			else if (m_dicName.ContainsKey("en-US"))
				strReturn = m_dicName["en-US"];

			return strReturn;
		}

		public int MultiverseId
		{
			get { return m_nMultiverseId; }
		}

		public string LocalizedFlavour
		{
			get
			{
				if (m_dicFlavourText != null)
				{
					if (m_dicFlavourText.ContainsKey(Settings.Language.LanguageCode))
						return m_dicFlavourText[Settings.Language.LanguageCode];
					else if ((Settings.Language.MasqueradeAsLangCode != null) && (m_dicFlavourText.ContainsKey(Settings.Language.MasqueradeAsLangCode)))
						return m_dicFlavourText[Settings.Language.MasqueradeAsLangCode];
					else
					{
						if (m_dicFlavourText.ContainsKey("en-US"))
							return m_dicFlavourText["en-US"];
						else
							return null;
					}
				}
				else
					return null;
			}
		}

		public Dictionary<string, string> Flavour
		{
			get { return m_dicFlavourText; }
		}

		public string PresentInWad
		{
			get { return m_strWadName; }
		}

		public string ImageFilename
		{
			get { return m_strImageName; }
		}

		public string CastingCost
		{
			get { return m_strCastingCost; }
		}

		public Image CastingCostImage
		{
			get
			{
				if (m_imgCastingCost == null)
				{
					// Create our cache if it doesn't exist.
					if (m_dicCastingCostCache == null)
						m_dicCastingCostCache = new Dictionary<string, Bitmap>();

					lock (m_dicCastingCostCache)
					{
						// See if we already have an image for this cost in the cache.
						if ((m_strCastingCost != null) && (m_strCastingCost.Length > 0))
						{
							if (m_dicCastingCostCache.ContainsKey(m_strCastingCost))
								m_imgCastingCost = m_dicCastingCostCache[m_strCastingCost];
						}
						else
						{
							if (m_dicCastingCostCache.ContainsKey("None"))
								m_imgCastingCost = m_dicCastingCostCache["None"];
						}
					}

					// Make sure we have a reference to the game directory and our image reference is still null.
					if ((m_gdData != null) && (m_imgCastingCost == null))
					{
						// Try to create an image of the casting cost for display purposes.
						List<string> lstTokens = Tools.CostToTokenList(m_strCastingCost);
						if (lstTokens.Count > 0)
						{
							Point ptLocation = new Point(0, 0);
							Bitmap bmpImage = new Bitmap(m_nManaIconShiftX * lstTokens.Count, m_nManaIconShiftY);
                            using (Graphics grfx = Graphics.FromImage(bmpImage))
                            {
                                grfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                foreach (string strToken in lstTokens)
                                {
                                    string strImg = Tools.CostTokenToImageName(strToken);
                                    TdxWrapper twImage = m_gdData.LoadImage(strImg, LoadImageType.Mana);
                                    if (twImage != null)
                                    {
                                        grfx.DrawImage(twImage.Image, new Rectangle(ptLocation.X + m_ptManaIconOffset.X, ptLocation.Y + m_ptManaIconOffset.Y, m_szManaIconTarget.Width, m_szManaIconTarget.Height));
                                        if (!Settings.GetSetting("MaintainImageCache", Settings.MAINTAIN_IMAGE_CACHE_DEFAULT))
                                            twImage.Dispose();
                                    }
                                    ptLocation.X += m_nManaIconShiftX;
                                }
                            }
                            if (m_imgCastingCost != null)
                                m_imgCastingCost.Dispose();
                            m_imgCastingCost = bmpImage;
						}
						else
						{
							m_imgCastingCost = new Bitmap(1, 1);
						}

						// At this point we now have an image for the casting cost, so we should
						//	save it's reference in the cache so we don't have to build it again.
						lock (m_dicCastingCostCache)
						{
							if ((m_strCastingCost != null) && (m_strCastingCost.Length > 0))
								m_dicCastingCostCache.Add(m_strCastingCost, m_imgCastingCost);
							else
								m_dicCastingCostCache.Add("None", m_imgCastingCost);
						}
					}
				}
				return m_imgCastingCost;
			}
		}

		public int ConvertedManaCost
		{
			get { return m_nCMC; }
		}

		public string CardXML
		{
			get { return m_strCardXML; }
		}

		public CardType Type
		{
			get { return m_eType; }
		}

		public ColourFlags Colour
		{
			get { return m_eColours; }
		}

		public CardRarity Rarity
		{
			get { return m_eRarity; }
		}

		public string ColourText
		{
			get { return Tools.ColourText(m_eColours); }
		}

		public CardSuperType SuperTypes
		{
			get { return m_eSuperType; }
		}

		public List<CardSubType> SubTypes
		{
			get { return m_lstSubTypes; }
		}

		public List<Ability> Abilities
		{
			get { return m_lstAbilities; }
		}

		public Dictionary<string, string> TypeLine
		{
			get { return m_dicTypeLine; }
		}

		public string LocalizedTypeLine
		{
			get
			{
				if (Settings.Language.MasqueradeAsLangCode != null)
					return GetTypeLine(Settings.Language.MasqueradeAsLangCode);
				else
					return GetTypeLine(Settings.Language.LanguageCode);
			}
		}

		public string GetTypeLine(string strLangCode)
		{
			if (m_dicTypeLine.ContainsKey(strLangCode))
				return m_dicTypeLine[strLangCode];
			else
				return GenerateTypeLine(strLangCode);
		}

		public string Power
		{
			get { return m_strPower; }
		}

		public string Toughness
		{
			get { return m_strToughness; }
		}

		public string PowerToughness
		{
			get
			{
				if (m_strPower != null)
					return m_strPower + " / " + m_strToughness;
				else
					return string.Empty;
			}
		}

		public Dictionary<string, string> AbilitiesText
		{
			get { return m_dicAbilitiesText; }
		}

		public string LocalizedAbilitiesText
		{
			get
			{
				if (Settings.Language.MasqueradeAsLangCode != null)
					return GetAbilitiesText(Settings.Language.MasqueradeAsLangCode);
				else
					return GetAbilitiesText(Settings.Language.LanguageCode);
			}
		}

		public string GetAbilitiesText(string strLangCode)
		{
			if (m_dicAbilitiesText.ContainsKey(strLangCode))
				return m_dicAbilitiesText[strLangCode];
			else
				return GenerateAbilitiesText(strLangCode);
		}

		public string Artist
		{
			get { return m_strArtist; }
		}

		public bool Token
		{
			get { return m_bToken; }
		}

        public bool Underscore
        {
            get { return m_bUnderscore; }
        }

		public bool MustBeIncludedInWad
		{
			get { return m_bMustBeIncludedInWad; }
		}

		public string Expansion
		{
			get { return m_strExpansion; }
		}

		public SortedSet<string> RegisteredTokens
		{
			get { return m_setRegisteredTokens; }
		}

		public string RegisteredTokensText
		{
			get
			{
				string strList = string.Empty;

				foreach (string strToken in m_setRegisteredTokens)
				{
					if (strList.Length > 0)
						strList += ", ";
					strList += strToken;
				}

				return strList;
			}
		}

		public bool HybridMana
		{
			get { return m_bIsHybrid; }
		}

		public bool PhyrexianMana
		{
			get { return m_bIsPhyrexian; }
		}

		public string ExtraFrameType
		{
			get { return m_strFrameType; }
		}

        public CardInfo ReverseFace
        {
            get {
                if (m_strFlipFileName != null && !m_strFlipFileName.Equals(String.Empty))
                    return m_gdData.GetCardByFileName(m_strFlipFileName);
                else
                    return null;
            }
        }

        public List<Legality> Legalities
        {
            get { return m_legLegalities; }
        }

        public string FormatLegality
        {
            get
            {
                var CurrentFormat = Settings.GetSerializableSetting("CurrentFormat", "None");
                if (CurrentFormat == "None")
                {
                    return "";
                }
                else
                {
                    var legality = m_legLegalities?.Where(L => L.Format == CurrentFormat);
                    if (legality != null)
                    {
                        if (legality.Any())
                            return legality.First().Status.ToString();
                        else
                            return "Banned";
                    }
                }
                return "Unknown";
            }
        }

        public Dictionary<string, SortableBindingList<string>> CustomTags
		{
			get { return m_dicCustomTags; }
		}

		public SortableBindingList<string> GetCustomTag(string strKey)
		{
			if (m_dicCustomTags.ContainsKey(strKey))
				return m_dicCustomTags[strKey];

			return null;
		}

		public string GetFirstCustomTagValue(string strKey)
		{
			if (m_dicCustomTags.ContainsKey(strKey))
			{
				SortableBindingList<string> lstValues = m_dicCustomTags[strKey];
				if (lstValues != null)
					return lstValues[0];
			}

			return string.Empty;
		}

		public KeyValuePair<string, string> DetermineFrameAndBox()
		{
			string strArtifact = string.Empty;
			string strFrame = string.Empty;
			string strToken = string.Empty;
            string strUnderscore = string.Empty;
			string strHybrid = string.Empty;
			string strBox = null;

			// Check for Basic Land
			//if (((m_eCardType & CardType.Land) == CardType.Land) && (m_setSuperTypes.Contains("BASIC")))
			if (((m_eType & CardType.Land) == CardType.Land) && ((m_eSuperType & CardSuperType.Basic) == CardSuperType.Basic))
			{
				foreach (CardSubType cstType in m_lstSubTypes)
				{
					if (cstType.SubType.Equals("FOREST"))
					{
						strFrame = "G_BASIC_LAND";
						break;
					}
					else if (cstType.SubType.Equals("ISLAND"))
					{
						strFrame = "U_BASIC_LAND";
						break;
					}
					else if (cstType.SubType.Equals("MOUNTAIN"))
					{
						strFrame = "R_BASIC_LAND";
						break;
					}
					else if (cstType.SubType.Equals("PLAINS"))
					{
						strFrame = "W_BASIC_LAND";
						break;
					}
					else if (cstType.SubType.Equals("SWAMP"))
					{
						strFrame = "B_BASIC_LAND";
						break;
					}
				}
			}
			else
			{
				if (m_bToken)
					strToken = "_TOKEN2";

                if (m_bUnderscore)
                    strUnderscore = "_UNDERSCORE";

				// Check for Artifact frame
				if ((m_eType & CardType.Artifact) == CardType.Artifact)
					strArtifact = "_ARTIFACT";

				// Now figure out colours
				if ((m_eColours & ColourFlags.MultiColour) == ColourFlags.MultiColour)
				{
					//BG,BR,RG,UB,UG,UR,WB,WG,WR,WU,Z
					if (((m_eColours & ColourFlags.Black) == ColourFlags.Black) &&
						((m_eColours & ColourFlags.Blue) != ColourFlags.Blue) &&
						((m_eColours & ColourFlags.Green) == ColourFlags.Green) &&
						((m_eColours & ColourFlags.Red) != ColourFlags.Red) &&
						((m_eColours & ColourFlags.White) != ColourFlags.White))
						strFrame = "BG";
					else if (((m_eColours & ColourFlags.Black) == ColourFlags.Black) &&
						((m_eColours & ColourFlags.Blue) != ColourFlags.Blue) &&
						((m_eColours & ColourFlags.Green) != ColourFlags.Green) &&
						((m_eColours & ColourFlags.Red) == ColourFlags.Red) &&
						((m_eColours & ColourFlags.White) != ColourFlags.White))
						strFrame = "BR";
					else if (((m_eColours & ColourFlags.Black) != ColourFlags.Black) &&
						((m_eColours & ColourFlags.Blue) != ColourFlags.Blue) &&
						((m_eColours & ColourFlags.Green) == ColourFlags.Green) &&
						((m_eColours & ColourFlags.Red) == ColourFlags.Red) &&
						((m_eColours & ColourFlags.White) != ColourFlags.White))
						strFrame = "RG";
					else if (((m_eColours & ColourFlags.Black) == ColourFlags.Black) &&
						((m_eColours & ColourFlags.Blue) == ColourFlags.Blue) &&
						((m_eColours & ColourFlags.Green) != ColourFlags.Green) &&
						((m_eColours & ColourFlags.Red) != ColourFlags.Red) &&
						((m_eColours & ColourFlags.White) != ColourFlags.White))
						strFrame = "UB";
					else if (((m_eColours & ColourFlags.Black) != ColourFlags.Black) &&
						((m_eColours & ColourFlags.Blue) == ColourFlags.Blue) &&
						((m_eColours & ColourFlags.Green) == ColourFlags.Green) &&
						((m_eColours & ColourFlags.Red) != ColourFlags.Red) &&
						((m_eColours & ColourFlags.White) != ColourFlags.White))
						strFrame = "UG";
					else if (((m_eColours & ColourFlags.Black) != ColourFlags.Black) &&
						((m_eColours & ColourFlags.Blue) == ColourFlags.Blue) &&
						((m_eColours & ColourFlags.Green) != ColourFlags.Green) &&
						((m_eColours & ColourFlags.Red) == ColourFlags.Red) &&
						((m_eColours & ColourFlags.White) != ColourFlags.White))
						strFrame = "UR";
					else if (((m_eColours & ColourFlags.Black) == ColourFlags.Black) &&
						((m_eColours & ColourFlags.Blue) != ColourFlags.Blue) &&
						((m_eColours & ColourFlags.Green) != ColourFlags.Green) &&
						((m_eColours & ColourFlags.Red) != ColourFlags.Red) &&
						((m_eColours & ColourFlags.White) == ColourFlags.White))
						strFrame = "WB";
					else if (((m_eColours & ColourFlags.Black) != ColourFlags.Black) &&
						((m_eColours & ColourFlags.Blue) != ColourFlags.Blue) &&
						((m_eColours & ColourFlags.Green) == ColourFlags.Green) &&
						((m_eColours & ColourFlags.Red) != ColourFlags.Red) &&
						((m_eColours & ColourFlags.White) == ColourFlags.White))
						strFrame = "WG";
					else if (((m_eColours & ColourFlags.Black) != ColourFlags.Black) &&
						((m_eColours & ColourFlags.Blue) != ColourFlags.Blue) &&
						((m_eColours & ColourFlags.Green) != ColourFlags.Green) &&
						((m_eColours & ColourFlags.Red) == ColourFlags.Red) &&
						((m_eColours & ColourFlags.White) == ColourFlags.White))
						strFrame = "WR";
					else if (((m_eColours & ColourFlags.Black) != ColourFlags.Black) &&
						((m_eColours & ColourFlags.Blue) == ColourFlags.Blue) &&
						((m_eColours & ColourFlags.Green) != ColourFlags.Green) &&
						((m_eColours & ColourFlags.Red) != ColourFlags.Red) &&
						((m_eColours & ColourFlags.White) == ColourFlags.White))
						strFrame = "WU";
					else
						strFrame = "Z";
					strBox = "PTBOX_GOLD";
					// Check for special Hybrid case
					if ((!m_bToken) && (m_bIsHybrid) && (!strFrame.Equals("Z")))
						strHybrid = "_HYBRID";
				}
				else if ((m_eColours & ColourFlags.Colourless) == ColourFlags.Colourless)
				{
					if ((m_eType & CardType.Land) == CardType.Land)
					{
						if (strArtifact.Length != 0)
						{
							strFrame = "C";
							strBox = "PTBOX_A";
						}
						else
							strFrame = "C_LAND";
					}
					else
					{
						strFrame = "C";
						strBox = "PTBOX_C";
					}
				}
				else if ((m_eColours & ColourFlags.Black) == ColourFlags.Black)
				{
					strFrame = "B";
					strBox = "PTBOX_B";
				}
				else if ((m_eColours & ColourFlags.Blue) == ColourFlags.Blue)
				{
					strFrame = "U";
					strBox = "PTBOX_U";
				}
				else if ((m_eColours & ColourFlags.Green) == ColourFlags.Green)
				{
					strFrame = "G";
					strBox = "PTBOX_G";
				}
				else if ((m_eColours & ColourFlags.Red) == ColourFlags.Red)
				{
					strFrame = "R";
					strBox = "PTBOX_R";
				}
				else if ((m_eColours & ColourFlags.White) == ColourFlags.White)
				{
					strFrame = "W";
					strBox = "PTBOX_W";
				}
			}
			return new KeyValuePair<string, string>(strFrame + strArtifact + strToken + strHybrid + (m_strFrameType.Length > 0 ? "_" + m_strFrameType : string.Empty), strBox);
		}

		public Bitmap GetLocalizedPreviewImage()
		{
			if (Settings.Language.MasqueradeAsLangCode != null)
				return GetPreviewImage(Settings.Language.MasqueradeAsLangCode);
			else
				return GetPreviewImage(Settings.Language.LanguageCode);
		}

		public Bitmap GetPreviewImage(string strLangCode)
		{
			Bitmap imgPreview = null;

			if (m_dicCardPreview.ContainsKey(strLangCode))
				imgPreview = m_dicCardPreview[strLangCode];
			else
				imgPreview = GeneratePreviewImage(strLangCode);

			return imgPreview;
		}

		private Bitmap GeneratePreviewImage(string strLangCode)
		{
			Bitmap bmpImage = null;

			if (m_gdData != null)
			{
				lock (m_dicCardPreview)
				{
					if (m_sfFullCenter == null)
					{
						m_sfFullCenter = new StringFormat();
						m_sfFullCenter.Alignment = StringAlignment.Center;
						m_sfFullCenter.LineAlignment = StringAlignment.Center;
					}
					if (m_sfLeftCenter == null)
					{
						m_sfLeftCenter = new StringFormat();
						m_sfLeftCenter.Alignment = StringAlignment.Near;
						m_sfLeftCenter.LineAlignment = StringAlignment.Center;
						m_sfLeftCenter.Trimming = StringTrimming.EllipsisCharacter;
					}
					if (m_sfCardText == null)
					{
						m_sfCardText = new StringFormat();
						m_sfCardText.Alignment = StringAlignment.Near;
						m_sfCardText.LineAlignment = StringAlignment.Near;
					}

					KeyValuePair<string, string> kvFrameBox = DetermineFrameAndBox();
					TdxWrapper twFrame = m_gdData.LoadImage(kvFrameBox.Key, LoadImageType.Frame);
					TdxWrapper twPTBox = m_gdData.LoadImage(kvFrameBox.Value, LoadImageType.Texture);
					TdxWrapper twImage = m_gdData.LoadImage(ImageFilename, LoadImageType.Card);

					bmpImage = new Bitmap(356, 512);

					if (twFrame != null)
					{
						if (twFrame.Image.Width > twFrame.Image.Height)
							twFrame.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
					}

                    using (Graphics grfx = Graphics.FromImage(bmpImage))
                    {
                        grfx.Clear(Color.Black);
                        grfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                        if ((m_eColours == ColourFlags.Colourless) && ((m_eType & CardType.Artifact) != CardType.Artifact) && ((m_eType & CardType.Land) != CardType.Land))
                        {
                            if (twImage != null)
                                grfx.DrawImage(twImage.Image, m_rcCardImageFull);
                            if (twFrame != null)
                                grfx.DrawImage(twFrame.Image, m_rcCardImageFull);
                            if (m_bToken)
                            {
                                // PT Box gets double duty as Card Name Box for tokens.
                                if (twPTBox != null)
                                    grfx.DrawImage(twPTBox.Image, m_rcCardPTBoxAsName);
                            }
                        }
                        else if (!m_bToken)
                        {
                            if (twFrame != null)
                                grfx.DrawImage(twFrame.Image, m_rcCardImageFull);
                            if (twImage != null)
                                grfx.DrawImage(twImage.Image, m_rcCardImagePicture);
                        }
                        else
                        {
                            if (twImage != null)
                                grfx.DrawImage(twImage.Image, m_rcCardImageFull);
                            if (twFrame != null)
                                grfx.DrawImage(twFrame.Image, m_rcCardImageFull);
                            // PT Box gets double duty as Card Name Box for tokens.
                            if (twPTBox != null)
                                grfx.DrawImage(twPTBox.Image, m_rcCardPTBoxAsName);
                        }

                        int nCostWidth = 0;
                        if (m_strCastingCost != null)
                        {
                            List<string> lstTokens = Tools.CostToTokenList(m_strCastingCost);
                            if (lstTokens.Count > 0)
                            {
                                int i = 0;
                                nCostWidth = m_nManaIconShiftX * lstTokens.Count;
                                foreach (string strToken in lstTokens)
                                {
                                    string strImg = Tools.CostTokenToImageName(strToken);
                                    TdxWrapper twCost = m_gdData.LoadImage(strImg, LoadImageType.Mana);
                                    if (twCost != null)
                                    {
                                        grfx.DrawImage(twCost.Image, new Rectangle(m_ptManaIconStart.X - (m_nManaIconShiftX * (lstTokens.Count - i)), m_ptManaIconStart.Y, m_szManaIconTarget.Width, m_szManaIconTarget.Height));
                                        if (!Settings.GetSetting("MaintainImageCache", Settings.MAINTAIN_IMAGE_CACHE_DEFAULT))
                                            twCost.Dispose();
                                    }
                                    i++;
                                }
                            }
                        }

                        if (m_bToken)
                        {
                            grfx.DrawString(GetLocalizationIfExists(m_dicName, strLangCode), m_ftCardNameFont, m_brCardTextBrush, m_rcCardNameToken, m_sfFullCenter);
                            grfx.DrawString(GetTypeLine(strLangCode), m_ftCardTypeFont, m_brCardTextBrush, m_rcCardTypeLineToken, m_sfLeftCenter);
                            DrawCardText(grfx, m_rcCardTextToken, m_ftCardTextFontRegular, m_ftCardTextFontItalic, m_brCardTextBrush, m_gdData, GetAbilitiesText(strLangCode));
                        }
                        else
                        {
                            Rectangle rcNameLimit = m_rcCardNameRegular;
                            rcNameLimit.Width -= nCostWidth;
                            grfx.DrawString(GetLocalizationIfExists(m_dicName, strLangCode), m_ftCardNameFont, m_brCardTextBrush, rcNameLimit, m_sfLeftCenter);
                            grfx.DrawString(GetTypeLine(strLangCode), m_ftCardTypeFont, m_brCardTextBrush, m_rcCardTypeLineRegular, m_sfLeftCenter);
                            string strLocalizedFlavour = GetLocalizationIfExists(m_dicFlavourText, strLangCode);
                            string strLocalizedAbilities = GetAbilitiesText(strLangCode);
                            if ((strLocalizedAbilities.Length == 0) && (strLocalizedFlavour != null) && (strLocalizedFlavour.Length > 0))
                                DrawCardText(grfx, m_rcCardTextRegular, m_ftCardTextFontRegular, m_ftCardTextFontItalic, m_brCardTextBrush, m_gdData, "|" + strLocalizedFlavour + "|");
                            else if ((strLocalizedFlavour != null) && (strLocalizedFlavour.Length > 0))
                                DrawCardText(grfx, m_rcCardTextRegular, m_ftCardTextFontRegular, m_ftCardTextFontItalic, m_brCardTextBrush, m_gdData, strLocalizedAbilities + "\r\n|" + strLocalizedFlavour + "|");
                            else
                                DrawCardText(grfx, m_rcCardTextRegular, m_ftCardTextFontRegular, m_ftCardTextFontItalic, m_brCardTextBrush, m_gdData, strLocalizedAbilities);
                        }

                        // Load up the P/T box if applicable.
                        if ((m_eType & CardType.Creature) == CardType.Creature)
                        {
                            if (twPTBox != null)
                                grfx.DrawImage(twPTBox.Image, m_rcCardPTBox);
                            grfx.DrawString(PowerToughness, m_ftCardNameFont, m_brCardTextBrush, m_rcCardPTBoxText, m_sfFullCenter);
                        }

                        // Artist Credit
                        string strCreditImage = "CREDIT_BLACK";
                        Brush brArtistBrush = m_brCardTextBrush;
                        if ((kvFrameBox.Key.StartsWith("B", StringComparison.OrdinalIgnoreCase)) ||
                            (kvFrameBox.Key.Equals("C_LAND", StringComparison.OrdinalIgnoreCase)))
                        {
                            strCreditImage = "CREDIT_WHITE";
                            brArtistBrush = m_brCardArtistWhite;
                        }
                        TdxWrapper twCredit = m_gdData.LoadImage(strCreditImage, LoadImageType.Texture);
                        if (twCredit != null)
                            grfx.DrawImage(twCredit.Image, m_rcCardArtistImage);
                        grfx.DrawString(m_strArtist, m_ftCardArtistFont, brArtistBrush, m_ptArtistCredit);

                        // Now we can put on the expansion image (Only those images from DotP 2013 supported, sorry no real expansion images)
                        string strExpansion = Tools.CardRarityToImageName(m_eRarity);
                        if (strExpansion != null)
                        {
                            TdxWrapper twExpansion = m_gdData.LoadImage(strExpansion, LoadImageType.Texture);
                            if (twExpansion != null)
                            {
                                grfx.DrawImage(twExpansion.Image, m_rcCardExpansionImage);
                                if (!Settings.GetSetting("MaintainImageCache", Settings.MAINTAIN_IMAGE_CACHE_DEFAULT))
                                    twExpansion.Dispose();
                            }
                        }

                        if (Settings.GetSetting("MaintainImageCache", Settings.MAINTAIN_IMAGE_CACHE_DEFAULT))
                        {
                            // Now that we've generated the image we should store it so we don't need to re-generate.
                            if (m_dicCardPreview.ContainsKey(strLangCode))
                                m_dicCardPreview[strLangCode] = bmpImage;
                            else
                                m_dicCardPreview.Add(strLangCode, bmpImage);
                        }
                        else
                        {
                            // Since we aren't keeping the image cache then we need to dispose of all our images.
                            if (twFrame != null)
                                twFrame.Dispose();
                            if (twImage != null)
                                twImage.Dispose();
                            if (twPTBox != null)
                                twPTBox.Dispose();
                            if (twCredit != null)
                                twCredit.Dispose();
                        }
                    }
				}
			}

			return bmpImage;
		}

		private void DrawCardText(Graphics grfx, Rectangle rcBounds, Font ftRegular, Font ftItalic, Brush brBrush, GameDirectory gdData, string strText)
		{
			if ((strText != null) && (strText.Length > 0))
			{
				// Special StringFormat so we measure trailing spaces.
				StringFormat sfText = new StringFormat(StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap);
				sfText.Alignment = StringAlignment.Near;
				sfText.LineAlignment = StringAlignment.Near;
				sfText.Trimming = StringTrimming.None;
				// Our start location.
				PointF ptLocation = new Point(rcBounds.X, rcBounds.Y);
				// We're going to be adjusting our string word by word in some cases.
				string strTextLeft = strText;
				// Just in case we run out of room in the bounding box before we finish.
				bool bFinished = false;

				Font ftCurrent = ftRegular;

				while ((strTextLeft.Length > 0) && (!bFinished))
				{
					int nLength = 0;
					// First thing is first we need to see how this string starts.
					if (strTextLeft.StartsWith("{"))
					{
						// We start off with a token because it is a token we should not have
						//	anything else between the start and end of the token to worry about.
						nLength = strTextLeft.IndexOf("}") + 1;
						if (nLength > 0)
						{
							string strTokenImage = Tools.TextTokenToImageName(strTextLeft.Substring(0, nLength));
							TdxWrapper twImage = gdData.LoadImage(strTokenImage);
							if (twImage != null)
							{
								if (!strTokenImage.Equals("CHAOS_UNLOCK_SYMBOL", StringComparison.OrdinalIgnoreCase))
								{
									// Make sure we are within bounds.
									if ((ptLocation.X + m_nManaTextShift) > rcBounds.Right)
									{
										// We reached the end of the line and need to move down and back
										ptLocation.X = rcBounds.X;
										ptLocation.Y += m_nManaTextShift;
									}
									if ((ptLocation.Y + m_nManaTextShift) <= rcBounds.Bottom)
									{
										grfx.DrawImage(twImage.Image, new Rectangle((int)ptLocation.X + m_ptManaTextOffset.X, (int)ptLocation.Y + m_ptManaTextOffset.Y, m_szManaTextTarget.Width, m_szManaTextTarget.Height));
										ptLocation.X += m_nManaTextShift;

										// Adjust for an issue with text after an image.
										if (strTextLeft.Length > nLength)
											if (!strTextLeft.Substring(nLength, 1).Equals("{"))
												ptLocation.X += (m_nTextShiftOffset / 2);
									}
									else
									{
										// We ran out of room in the bounding box to draw.
										bFinished = true;
									}
								}
								else
								{
									// Special Case for Chaos Image on Planes
									// Make sure we are within bounds.
									if ((ptLocation.X + m_nChaosTextXShift) > rcBounds.Right)
									{
										// We reached the end of the line and need to move down and back
										ptLocation.X = rcBounds.X;
										ptLocation.Y += m_nChaosTextYShift;
									}
									if ((ptLocation.Y + m_nChaosTextYShift) <= rcBounds.Bottom)
									{
										grfx.DrawImage(twImage.Image, new Rectangle((int)ptLocation.X + m_ptChaosOffset.X, (int)ptLocation.Y + m_ptChaosOffset.Y, m_szChaosTarget.Width, m_szChaosTarget.Height));
										ptLocation.X += m_nChaosTextXShift;

										// Adjust for an issue with text after an image.
										if (!strTextLeft.Substring(nLength, 1).Equals("{"))
											ptLocation.X += (m_nTextShiftOffset / 2);
									}
									else
									{
										// We ran out of room in the bounding box to draw.
										bFinished = true;
									}
								}
								if (!Settings.GetSetting("MaintainImageCache", Settings.MAINTAIN_IMAGE_CACHE_DEFAULT))
									twImage.Dispose();
							}
						}
						else
						{
							// Um, there is a problem we have an unclosed token.
							//TODO: Handle this better.
							bFinished = true;
						}
					}
					else if (strTextLeft.StartsWith("|"))
					{
						// Toggle italics.
						if (ftCurrent == ftRegular)
							ftCurrent = ftItalic;
						else
							ftCurrent = ftRegular;
						nLength = 1;
					}
					else if (strTextLeft.StartsWith("\r\n"))
					{
						// This case happens when a line ends with a token for example Kickers (i.e. "Kicker {3}\r\n").
						//	So we need to adjust to the next line by approximating where it should go.
						SizeF szApprox = grfx.MeasureString("A", ftCurrent, ptLocation, sfText);
						ptLocation.X = rcBounds.X;
						if (ftCurrent != ftItalic)
							ptLocation.Y += szApprox.Height + m_nHardReturnBuffer;
						else
							ptLocation.Y += szApprox.Height;
						nLength = 2;
					}
					else
					{
						if (strTextLeft.Length == 0)
						{
							bFinished = true;
							break;
						}
						// We have plain text to deal with we have to check to see when we hit the end of line, a token, or an italic toggle and see which comes first.
						int nNextEoL = strTextLeft.IndexOf("\r\n");
						int nNextToken = strTextLeft.IndexOf("{");
						int nNextItalic = strTextLeft.IndexOf("|");
						int nFirstItem = nNextEoL;
						if ((nFirstItem == -1) || ((nNextToken > -1) && (nNextToken < nFirstItem)))
							nFirstItem = nNextToken;
						if ((nFirstItem == -1) || ((nNextItalic > -1) && (nNextItalic < nFirstItem)))
							nFirstItem = nNextItalic;
						if (nFirstItem == -1)
						{
							// We have no EoLs, no tokens, and no italic toggles left so our max limit is end of string.
							nFirstItem = strTextLeft.Length;
						}
						SizeF szSpaceRequired = grfx.MeasureString(strTextLeft.Substring(0, nFirstItem), ftCurrent, ptLocation, sfText);
						// Verify we can draw in the bounds area.
						if ((ptLocation.X + szSpaceRequired.Width + m_nTextShiftOffset) > rcBounds.Right)
						{
							// If I were to draw the string now it would go beyond our bounds so we'll try doing just a single word or part of a hyphenated word.
							int nWordBoundary = strTextLeft.IndexOf(" ");
							int nHyphen = strTextLeft.IndexOf("-");

							if ((nWordBoundary > -1) && (nWordBoundary < nFirstItem))
								nFirstItem = nWordBoundary + 1;
							if ((nHyphen > -1) && (nHyphen < nFirstItem))
								nFirstItem = nHyphen + 1;

							szSpaceRequired = grfx.MeasureString(strTextLeft.Substring(0, nFirstItem), ftCurrent, ptLocation, sfText);
							// Check again.
							if ((ptLocation.X + szSpaceRequired.Width + m_nTextShiftOffset) > rcBounds.Right)
							{
								PointF ptOrig = ptLocation;
								// We're too close to the edge to draw anything so we're going to move down and back which should get us past this check.
								ptLocation.X = rcBounds.X;
								ptLocation.Y += szSpaceRequired.Height;
								// Now unless we will go past the bottom of the box we should be good (but check width one last time to be sure).
								szSpaceRequired = grfx.MeasureString(strTextLeft.Substring(0, nFirstItem), ftCurrent, ptLocation, sfText);
								if ((ptLocation.X + szSpaceRequired.Width + m_nTextShiftOffset) > rcBounds.Right)
								{
									// Still to wide I'm assuming we're dealing with foreign text now and
									//	since I don't have a way to detect word boundarys with foreign text
									//	(like Japanese and Korean) I'm going to have to shorten it one
									//	character at a time until I get it to fit.
									// If we still had space on the last line go back there.
									if ((ptOrig.X + m_nSpaceRemainingBuffer) <= rcBounds.Right)
										ptLocation = ptOrig;
									nFirstItem--;
									while (nFirstItem > 0)
									{
										szSpaceRequired = grfx.MeasureString(strTextLeft.Substring(0, nFirstItem), ftCurrent, ptLocation, sfText);
										if ((ptLocation.X + szSpaceRequired.Width + m_nTextShiftOffset) <= rcBounds.Right)
											break;
										nFirstItem--;
									}
								}
							}
						}
						// We now have an acceptable length (in nFirstItem).
						nLength = nFirstItem;
						if ((ptLocation.Y + szSpaceRequired.Height) <= rcBounds.Bottom)
						{
							// We're good so draw the text.
							grfx.DrawString(strTextLeft.Substring(0, nFirstItem), ftCurrent, brBrush, ptLocation, sfText);
							// Adjust location with how much we drew.
							ptLocation.X += szSpaceRequired.Width + m_nTextShiftOffset;
						}
						else
						{
							// We've run out of space to draw.
							//TODO: Handle this better.
							bFinished = true;
						}
						if (nLength == nNextEoL)
						{
							// Now we need to adjust for a line break.
							ptLocation.X = rcBounds.X;
							if (ftCurrent != ftItalic)
								ptLocation.Y += szSpaceRequired.Height + m_nHardReturnBuffer;
							else
								ptLocation.Y += szSpaceRequired.Height;
							nLength += 2;
						}
						if (nLength == nNextToken)
						{
							// To make a token look right after drawn text we need to make a slight adjustment.
							ptLocation.X -= (m_nTextShiftOffset / 2);
						}
					}
					strTextLeft = strTextLeft.Substring(nLength);
				}
			}
		}

		private void ParseXML(string strXML)
		{
			XmlDocument xdDoc = new XmlDocument();
			xdDoc.LoadXml(strXML);
			foreach (XmlNode xnRoot in xdDoc.ChildNodes)
			{
				// Technically we should only have the CARD_V2 node and we should have only one, but check just to make sure.
				if (xnRoot.Name.Equals("CARD_V2", StringComparison.OrdinalIgnoreCase))
				{
					// I'm doing this differently than before so I can support custom tags.
					m_eSuperType = CardSuperType.None;
					m_eType = CardType.None;
					int nSubTypes = 0;
					int nSubTypeCount = XmlTools.CountTags(xnRoot, "SUB_TYPE");
					foreach (XmlNode xnNode in xnRoot.ChildNodes)
					{
						if (xnNode.Name.Equals("FILENAME", StringComparison.OrdinalIgnoreCase))
						{
							m_strFilename = XmlTools.GetValueFromAttribute(xnNode, "text");
                            m_bUnderscore = m_strFilename[0] == '_';
                        }
						else if (xnNode.Name.Equals("CARDNAME", StringComparison.OrdinalIgnoreCase))
							m_strCardName = XmlTools.GetValueFromAttribute(xnNode, "text");
						else if (xnNode.Name.Equals("TITLE", StringComparison.OrdinalIgnoreCase))
							m_dicName = XmlTools.GetLocalizedText(xnNode);
						else if (xnNode.Name.Equals("FLAVOURTEXT", StringComparison.OrdinalIgnoreCase))
							m_dicFlavourText = XmlTools.GetLocalizedText(xnNode);
						else if (xnNode.Name.Equals("ARTID", StringComparison.OrdinalIgnoreCase))
							m_strImageName = XmlTools.GetValueFromAttribute(xnNode, "value");
						else if (xnNode.Name.Equals("CASTING_COST", StringComparison.OrdinalIgnoreCase))
						{
							m_strCastingCost = XmlTools.GetValueFromAttribute(xnNode, "cost");
							m_nCMC = Tools.CastingCostToCMC(m_strCastingCost);
						}
						else if (xnNode.Name.Equals("COLOUR", StringComparison.OrdinalIgnoreCase))
							m_strColour = XmlTools.GetValueFromAttribute(xnNode, "value");
						else if (xnNode.Name.Equals("FRAME_TYPE", StringComparison.OrdinalIgnoreCase))
							m_strFrameType = XmlTools.GetValueFromAttribute(xnNode, "type").ToUpper();
						else if (xnNode.Name.Equals("SUPERTYPE", StringComparison.OrdinalIgnoreCase))
						{
							string strType = XmlTools.GetValueFromAttribute(xnNode, "metaname");
							foreach (CardSuperType cstType in Enum.GetValues(typeof(CardSuperType)))
							{
								if (cstType.ToString().Equals(strType, StringComparison.OrdinalIgnoreCase))
								{
									m_eSuperType |= cstType;
									break;
								}
							}
						}
						else if (xnNode.Name.Equals("TYPE", StringComparison.OrdinalIgnoreCase))
						{
							string strType = XmlTools.GetValueFromAttribute(xnNode, "metaname");
							foreach (CardType ctType in Enum.GetValues(typeof(CardType)))
							{
								if (ctType.ToString().Equals(strType, StringComparison.OrdinalIgnoreCase))
								{
									m_eType |= ctType;
									break;
								}
							}
						}
						else if (xnNode.Name.Equals("SUB_TYPE", StringComparison.OrdinalIgnoreCase))
						{
							CardSubType cstType = new CardSubType(xnNode, nSubTypes, nSubTypeCount);
							if (cstType.SubType.Length > 0)
							{
								m_lstSubTypes.Add(cstType);
								nSubTypes++;
							}
						}
						else if (xnNode.Name.Equals("TOKEN_REGISTRATION", StringComparison.OrdinalIgnoreCase))
						{
							XmlAttribute xaAttr = XmlTools.FindCaseInsensitiveAttribute(xnNode, "type");
							if (xaAttr != null)
								m_setRegisteredTokens.Add(xaAttr.Value.Trim());
						}
						else if (xnNode.Name.Equals("POWER", StringComparison.OrdinalIgnoreCase))
							m_strPower = XmlTools.GetValueFromAttribute(xnNode, "value");
						else if (xnNode.Name.Equals("TOUGHNESS", StringComparison.OrdinalIgnoreCase))
							m_strToughness = XmlTools.GetValueFromAttribute(xnNode, "value");
						else if (xnNode.Name.Equals("TOKEN", StringComparison.OrdinalIgnoreCase))
							m_bToken = true;
						else if (xnNode.Name.Equals("MULTIVERSEID", StringComparison.OrdinalIgnoreCase))
						{
							string strValue = XmlTools.GetValueFromAttribute(xnNode, "value");
							if (!Int32.TryParse(strValue, out m_nMultiverseId))
								Settings.ReportError(null, ErrorPriority.Low, "Card (" + m_strFilename + " in " + m_strWadName + ") has a multiverse id that is problematic: " + strValue);
						}
						else if (xnNode.Name.Equals("EXPANSION", StringComparison.OrdinalIgnoreCase))
							m_strExpansion = XmlTools.GetValueFromAttribute(xnNode, "value");
						else if (xnNode.Name.Equals("ARTIST", StringComparison.OrdinalIgnoreCase))
							m_strArtist = XmlTools.GetValueFromAttribute(xnNode, "name");
						else if (xnNode.Name.Equals("RARITY", StringComparison.OrdinalIgnoreCase))
						{
							string strRarity = XmlTools.GetValueFromAttribute(xnNode, "metaname");
							if (strRarity.Length > 0)
							{
								m_eRarity = CardRarity.None;
								switch (strRarity)
								{
									case "T": m_eRarity = CardRarity.Token; break;
									case "L": m_eRarity = CardRarity.Land; break;
									case "C": m_eRarity = CardRarity.Common; break;
									case "U": m_eRarity = CardRarity.Uncommon; break;
									case "R": m_eRarity = CardRarity.Rare; break;
									case "M": m_eRarity = CardRarity.Mythic; break;
									case "S": m_eRarity = CardRarity.Special; break;
								}
							}
						}
						else if (xnNode.Name.IndexOf("_ABILITY", StringComparison.OrdinalIgnoreCase) > -1)
						{
							try
							{
								Ability ability = new Ability(xnNode.OuterXml.Trim());
								m_lstAbilities.Add(ability);
							}
							catch (Exception e)
							{
								Settings.ReportError(e, ErrorPriority.Low, "Unable to load ability: " + xnNode.Name + " in " + m_strFilename);
							}
						}
						else if ((xnNode.Name.Equals("#comment", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("#text", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("HELP", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("SFX", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("AI_AVAILABILITY", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("AI_BASE_SCORE", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("AI_COUNTER_SCORE", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("AI_CUSTOM_SCORE", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("AI_DONT_DUPLICATE", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("AI_MODIFIERS", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("ANIMATEDARTFILENAME", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("SEALED_HEURISTICS", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("SEALED_SYNERGY_BONUS", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("SEALED_SYNERGY_TAG", StringComparison.OrdinalIgnoreCase))
							)
						{
						}
						else if ((xnNode.Name.Equals("AI_SIMPLIFIED_TARGETING", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("AUTO_SKIP", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("AVAILABILITY", StringComparison.OrdinalIgnoreCase)) ||
							(xnNode.Name.Equals("MAY", StringComparison.OrdinalIgnoreCase)))
						{
							// These tags are being used wrong.
							Settings.ReportError(null, ErrorPriority.Low, "Card (" + m_strActualFilename + ") in Wad " + m_strWadName + " is using the " + xnNode.Name + " tag incorrectly (will likely result in this piece not working properly in game).");
						}
                        else if (xnNode.Name.Equals("REVERSE_FACE", StringComparison.OrdinalIgnoreCase))
                        {
                            m_strFlipFileName = XmlTools.GetValueFromAttribute(xnNode, "value");
                        }
                        else if (xnNode.Name.Equals("LEGALITY", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                m_legLegalities = new List<Legality>();
                                foreach (XmlNode legNode in xnNode.ChildNodes)
                                {
                                    m_legLegalities.Add(new Legality()
                                    {
                                        Format = XmlTools.GetValueFromAttribute(legNode, "value"),
                                        Status = (LegalityValue)Enum.Parse(typeof(LegalityValue), XmlTools.GetValueFromAttribute(legNode, "status"))
                                    });
                                }
                            }
                            catch (Exception e)
                            {
                                Settings.ReportError(e, ErrorPriority.Low, "Unable to load legality: " + m_strFilename);
                            }
                        }
						else
						{
							// These are tags that I haven't accounted for and as such are loaded as custom tags.
							string strTagName = xnNode.Name.ToUpper();
							string strValue = xnNode.InnerText;
							if ((strValue != null) && (strValue.Length > 0))
							{
								SortableBindingList<string> lstValues = null;
								// We actually have a value for the custom tag so we should now look at saving it off.
								//	First check to see if we already have one.
								if (m_dicCustomTags.ContainsKey(strTagName))
								{
									// Repeat of a custom tag, though may have a different value.
									lstValues = m_dicCustomTags[strTagName];
									lstValues.Add(strValue);
								}
								else
								{
									// New custom tag
									lstValues = new SortableBindingList<string>();
									lstValues.Add(strValue);
									m_dicCustomTags.Add(strTagName, lstValues);
								}
							}
						}
					}

                    //Ensure we don't return planewalkers when filtering for enchantments.
					if (((m_eType & CardType.Planeswalker) == CardType.Planeswalker) && ((m_eType & CardType.Enchantment) == CardType.Enchantment))
                    {
                        m_eType &= ~CardType.Enchantment;
                    }

					// Now to check that we have a bare minimum of information to establish this as a card.
					if ((m_strFilename != null) && (m_dicName.Count > 0))
					{
						m_eColours = DetermineColour(m_strCastingCost, m_strColour);
					}
					else
					{
						// No, we don't have the minimum required information.
						throw new Exception("Failed to find required minimum information in Card:\n" +
							"\tFilename: " + (m_strFilename != null ? "Present (" + m_strFilename + ")" : "Missing") + 
							"\tName: " + (m_dicName.Count > 0 ? "Present (Languages: " + m_dicName.Count.ToString() + ")" : "Missing")
						);
					}
				}
			}
		}

		private ColourFlags DetermineColour(string strCastingCost, string strColour)
		{
			ColourFlags eColour = 0;

			// Do we have a Casting Cost?
			if ((strCastingCost != null) && (strCastingCost.Length > 0))
			{
				// We have a casting cost to process.
				if (strCastingCost.IndexOf("B") > -1)
					eColour |= ColourFlags.Black;
				if (strCastingCost.IndexOf("U") > -1)
					eColour |= ColourFlags.Blue;
				if (strCastingCost.IndexOf("G") > -1)
					eColour |= ColourFlags.Green;
				if (strCastingCost.IndexOf("R") > -1)
					eColour |= ColourFlags.Red;
				if (strCastingCost.IndexOf("W") > -1)
					eColour |= ColourFlags.White;

				// Determine if this uses hybrid mana
				if ((strCastingCost.IndexOf("{B/G}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{B/R}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{B/U}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{B/W}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{G/B}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{G/R}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{G/U}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{G/W}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{R/B}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{R/G}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{R/U}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{R/W}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{U/B}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{U/G}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{U/R}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{U/W}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{W/G}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{W/R}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{W/B}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{W/U}", StringComparison.OrdinalIgnoreCase) > -1))
					m_bIsHybrid = true;
				else
					m_bIsHybrid = false;

				// Determine whether this has phyrexian mana or not.
				if ((strCastingCost.IndexOf("{B/P}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{G/P}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{R/P}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{U/P}", StringComparison.OrdinalIgnoreCase) > -1) ||
					(strCastingCost.IndexOf("{W/P}", StringComparison.OrdinalIgnoreCase) > -1))
					m_bIsPhyrexian = true;
				else
					m_bIsPhyrexian = false;
			}

			// Were we given a colour string to process?
			if (strColour != null)
			{
				if (strColour.IndexOf("B") > -1)
					eColour |= ColourFlags.Black;
				if (strColour.IndexOf("U") > -1)
					eColour |= ColourFlags.Blue;
				if (strColour.IndexOf("G") > -1)
					eColour |= ColourFlags.Green;
				if (strColour.IndexOf("R") > -1)
					eColour |= ColourFlags.Red;
				if (strColour.IndexOf("W") > -1)
					eColour |= ColourFlags.White;
			}

			// Check to see if more than one colour on card.
			if ((((eColour & ColourFlags.Black) == ColourFlags.Black) &&
						(((eColour & ColourFlags.Blue) == ColourFlags.Blue) ||
						((eColour & ColourFlags.Green) == ColourFlags.Green) ||
						((eColour & ColourFlags.Red) == ColourFlags.Red) ||
						((eColour & ColourFlags.White) == ColourFlags.White))) ||
					(((eColour & ColourFlags.Blue) == ColourFlags.Blue) &&
						(((eColour & ColourFlags.Green) == ColourFlags.Green) ||
						((eColour & ColourFlags.Red) == ColourFlags.Red) ||
						((eColour & ColourFlags.White) == ColourFlags.White))) ||
					(((eColour & ColourFlags.Green) == ColourFlags.Green) &&
						(((eColour & ColourFlags.Red) == ColourFlags.Red) ||
						((eColour & ColourFlags.White) == ColourFlags.White))) ||
					(((eColour & ColourFlags.Red) == ColourFlags.Red) &&
						((eColour & ColourFlags.White) == ColourFlags.White)))
				eColour |= ColourFlags.MultiColour;

			// Check to see if there are no colours on card.
			if (eColour == 0)
				eColour = ColourFlags.Colourless;

			return eColour;
		}

		private string GenerateAbilitiesText(string strLangCode = null)
		{
			if (strLangCode == null)
			{
				if (Settings.Language.MasqueradeAsLangCode != null)
					strLangCode = Settings.Language.MasqueradeAsLangCode;
				else
					strLangCode = Settings.Language.LanguageCode;
			}

			string strCardAbilities = string.Empty;
			foreach (Ability ability in m_lstAbilities)
			{
				if ((ability.Text != null) && (ability.Text.Count > 0) && (ability.ResourceId < 0))
				{
					if (strCardAbilities.Length > 0)
					{
						if (ability.CommaSpace)
							strCardAbilities += ", ";
						else
							strCardAbilities += "\r\n";
					}
					if (ability.Text.ContainsKey(strLangCode))
						strCardAbilities += ability.Text[strLangCode].Trim();
					else if (ability.Text.ContainsKey("en-US"))
						strCardAbilities += ability.Text["en-US"].Trim();
				}
			}

			return strCardAbilities;
		}

		private string GenerateTypeLine(string strLangCode = null)
		{
			LanguageStrings lsStrings = Settings.UIStrings;
			if (strLangCode != null)
			{
				try
				{
					LanguageStrings lsLoad = new LanguageStrings(strLangCode);
					lsStrings = lsLoad;
				}
				catch (Exception e)
				{
					Settings.ReportError(e, ErrorPriority.Medium, "Could not load Localization strings for " + strLangCode);
				}
			}
			else
			{
				if (Settings.Language.MasqueradeAsLangCode != null)
					strLangCode = Settings.Language.MasqueradeAsLangCode;
				else
					strLangCode = Settings.Language.LanguageCode;
			}

			// Start by throwing in any supertypes.
			string strTypeLine = string.Empty;

			foreach (CardSuperType cstType in Enum.GetValues(typeof(CardSuperType)))
			{
				if (cstType != CardSuperType.None)
				{
					if ((m_eSuperType & cstType) == cstType)
						strTypeLine += lsStrings["CARD_SUPERTYPE_" + cstType.ToString().ToUpper()] + " ";
				}
			}

			// Now we construct an appropriate regular type from the CardType.
			// Tribal takes precedence if present.
			if ((m_eType & CardType.Tribal) == CardType.Tribal)
				strTypeLine += lsStrings["CARD_TYPE_" + CardType.Tribal.ToString().ToUpper()] + " ";
			CardType ctTest = CardType.Artifact;
			while (ctTest != CardType.Tribal)
			{
				if ((m_eType & ctTest) == ctTest)
					strTypeLine += lsStrings["CARD_TYPE_" + ctTest.ToString().ToUpper()] + " ";
				ctTest = (CardType)((int)ctTest << 1);
			}

			if (m_lstSubTypes.Count > 0)
			{
				strTypeLine += "— ";
				foreach (CardSubType cstSubType in m_lstSubTypes.OrderBy(x => x.OrderForLangCode(strLangCode)))
					strTypeLine += lsStrings["CARD_SUBTYPE_" + cstSubType.SubType.Replace(" ", "_")] + " ";
			}

			if (m_dicTypeLine.ContainsKey(strLangCode))
				m_dicTypeLine[strLangCode] = strTypeLine.Trim();
			else
				m_dicTypeLine.Add(strLangCode, strTypeLine.Trim());

			return strTypeLine.Trim();
		}

		private V GetLocalizationIfExists<V>(Dictionary<string, V> dic, string strLangCode)
		{
			if (dic.ContainsKey(strLangCode))
				return dic[strLangCode];
			else if (dic.ContainsKey("en-US"))
				return dic["en-US"];
			else
				return default(V);
		}

        public void Dispose()
        {
            if (m_dicCastingCostCache != null && m_dicCastingCostCache.Count > 0)
            {
                foreach (var B in m_dicCastingCostCache.Values)
                {
                    B.Dispose();
                }
                m_dicCastingCostCache.Clear();
                m_dicCastingCostCache = null;
            }
            if (m_imgCastingCost != null)
            {
                m_imgCastingCost.Dispose();
                m_imgCastingCost = null;
            }
            m_gdData = null;
            if (m_dicCardPreview != null && m_dicCardPreview.Count > 0)
            {
                foreach (var B in m_dicCardPreview.Values)
                {
                    B.Dispose();
                }
                m_dicCardPreview.Clear();
                m_dicCardPreview = null;
            }
            if (m_ftCardArtistFont != null)
            {
                m_ftCardArtistFont.Dispose();
                m_ftCardArtistFont = null;
            }
            if (m_ftCardNameFont != null)
            {
                m_ftCardNameFont.Dispose();
                m_ftCardNameFont = null;
            }
            if (m_ftCardTypeFont != null)
            {
                m_ftCardTypeFont.Dispose();
                m_ftCardTypeFont = null;
            }
            if (m_ftCardTextFontRegular != null)
            {
                m_ftCardTextFontRegular.Dispose();
                m_ftCardTextFontRegular = null;
            }
            if (m_ftCardTextFontItalic != null)
            {
                m_ftCardTextFontItalic.Dispose();
                m_ftCardTextFontItalic = null;
            }
            if (m_brCardTextBrush != null)
            {
                m_brCardTextBrush.Dispose();
                m_brCardTextBrush = null;
            }
            if (m_brCardArtistWhite != null)
            {
                m_brCardArtistWhite.Dispose();
                m_brCardArtistWhite = null;
            }
            if (m_sfFullCenter != null)
            {
                m_sfFullCenter.Dispose();
                m_sfFullCenter = null;
            }
            if (m_sfLeftCenter != null)
            {
                m_sfLeftCenter.Dispose();
                m_sfLeftCenter = null;
            }
            if (m_sfCardText != null)
            {
                m_sfCardText.Dispose();
                m_sfCardText = null;
            }
        }
	}
}
