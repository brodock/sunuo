using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server
{
	public class LootPack
	{
		private static int[] m_LuckTable = new int[]
			{
				100,  146,  184,  216,  244,  270,  294,  317,  338,  359,  378,  397,  415,  433,  450,  466,  482,  498,  513,  528,  542,  556,  570,  584,  597,  611,  624,  636,  649,  661,
				673,  685,  697,  709,  720,  732,  743,  754,  765,  776,  787,  797,  808,  818,  828,  838,  849,  859,  868,  878,  888,  898,  907,  917,  926,  935,  945,  954,  963,  972,
				981,  990,  999,  1007, 1016, 1025, 1033, 1042, 1050, 1059, 1067, 1076, 1084, 1092, 1100, 1108, 1116, 1125, 1133, 1140, 1148, 1156, 1164, 1172, 1180, 1187, 1195, 1203, 1210, 1218,
				1225, 1233, 1240, 1247, 1255, 1262, 1269, 1277, 1284, 1291, 1298, 1305, 1312, 1320, 1327, 1334, 1341, 1347, 1354, 1361, 1368, 1375, 1382, 1389, 1395, 1402, 1409, 1415, 1422, 1429,
				1435, 1442, 1448, 1455, 1462, 1468, 1474, 1481, 1487, 1494, 1500, 1506, 1513, 1519, 1525, 1532, 1538, 1544, 1550, 1557, 1563, 1569, 1575, 1581, 1587, 1593, 1599, 1605, 1611, 1617,
				1623, 1629, 1635, 1641, 1647, 1653, 1659, 1665, 1671, 1676, 1682, 1688, 1694, 1700, 1705, 1711, 1717, 1722, 1728, 1734, 1740, 1745, 1751, 1756, 1762, 1768, 1773, 1779, 1784, 1790,
				1795, 1801, 1806, 1812, 1817, 1823, 1828, 1834, 1839, 1844, 1850, 1855, 1861, 1866, 1871, 1877, 1882, 1887, 1892, 1898, 1903, 1908, 1914, 1919, 1924, 1929, 1934, 1940, 1945, 1950,
				1955, 1960, 1965, 1970, 1976, 1981, 1986, 1991, 1996, 2001, 2006, 2011, 2016, 2021, 2026, 2031, 2036, 2041, 2046, 2051, 2056, 2061, 2066, 2071, 2076, 2081, 2085, 2090, 2095, 2100,
				2105, 2110, 2115, 2119, 2124, 2129, 2134, 2139, 2143, 2148, 2153, 2158, 2163, 2167, 2172, 2177, 2181, 2186, 2191, 2196, 2200, 2205, 2210, 2214, 2219, 2224, 2228, 2233, 2238, 2242,
				2247, 2251, 2256, 2261, 2265, 2270, 2274, 2279, 2283, 2288, 2292, 2297, 2301, 2306, 2311, 2315, 2320, 2324, 2328, 2333, 2337, 2342, 2346, 2351, 2355, 2360, 2364, 2368, 2373, 2377,
				2382, 2386, 2390, 2395, 2399, 2404, 2408, 2412, 2417, 2421, 2425, 2430, 2434, 2438, 2443, 2447, 2451, 2456, 2460, 2464, 2468, 2473, 2477, 2481, 2485, 2490, 2494, 2498, 2502, 2507,
				2511, 2515, 2519, 2523, 2528, 2532, 2536, 2540, 2544, 2549, 2553, 2557, 2561, 2565, 2569, 2573, 2578, 2582, 2586, 2590, 2594, 2598, 2602, 2606, 2610, 2615, 2619, 2623, 2627, 2631,
				2635, 2639, 2643, 2647, 2651, 2655, 2659, 2663, 2667, 2671, 2675, 2679, 2683, 2687, 2691, 2695, 2699, 2703, 2707, 2711, 2715, 2719, 2723, 2727, 2731, 2735, 2739, 2743, 2747, 2750,
				2754, 2758, 2762, 2766, 2770, 2774, 2778, 2782, 2786, 2789, 2793, 2797, 2801, 2805, 2809, 2813, 2816, 2820, 2824, 2828, 2832, 2836, 2839, 2843, 2847, 2851, 2855, 2858, 2862, 2866,
				2870, 2874, 2877, 2881, 2885, 2889, 2893, 2896, 2900, 2904, 2908, 2911, 2915, 2919, 2922, 2926, 2930, 2934, 2937, 2941, 2945, 2949, 2952, 2956, 2960, 2963, 2967, 2971, 2974, 2978,
				2982, 2985, 2989, 2993, 2996, 3000, 3004, 3007, 3011, 3015, 3018, 3022, 3026, 3029, 3033, 3036, 3040, 3044, 3047, 3051, 3055, 3058, 3062, 3065, 3069, 3072, 3076, 3080, 3083, 3087,
				3090, 3094, 3098, 3101, 3105, 3108, 3112, 3115, 3119, 3122, 3126, 3129, 3133, 3137, 3140, 3144, 3147, 3151, 3154, 3158, 3161, 3165, 3168, 3172, 3175, 3179, 3182, 3186, 3189, 3193,
				3196, 3200, 3203, 3206, 3210, 3213, 3217, 3220, 3224, 3227, 3231, 3234, 3238, 3241, 3244, 3248, 3251, 3255, 3258, 3262, 3265, 3268, 3272, 3275, 3279, 3282, 3285, 3289, 3292, 3296,
				3299, 3302, 3306, 3309, 3312, 3316, 3319, 3323, 3326, 3329, 3333, 3336, 3339, 3343, 3346, 3349, 3353, 3356, 3360, 3363, 3366, 3370, 3373, 3376, 3379, 3383, 3386, 3389, 3393, 3396,
				3399, 3403, 3406, 3409, 3413, 3416, 3419, 3422, 3426, 3429, 3432, 3436, 3439, 3442, 3445, 3449, 3452, 3455, 3459, 3462, 3465, 3468, 3472, 3475, 3478, 3481, 3485, 3488, 3491, 3494,
				3497, 3501, 3504, 3507, 3510, 3514, 3517, 3520, 3523, 3527, 3530, 3533, 3536, 3539, 3543, 3546, 3549, 3552, 3555, 3559, 3562, 3565, 3568, 3571, 3574, 3578, 3581, 3584, 3587, 3590,
				3593, 3597, 3600, 3603, 3606, 3609, 3612, 3616, 3619, 3622, 3625, 3628, 3631, 3634, 3638, 3641, 3644, 3647, 3650, 3653, 3656, 3659, 3663, 3666, 3669, 3672, 3675, 3678, 3681, 3684,
				3687, 3690, 3694, 3697, 3700, 3703, 3706, 3709, 3712, 3715, 3718, 3721, 3724, 3728, 3731, 3734, 3737, 3740, 3743, 3746, 3749, 3752, 3755, 3758, 3761, 3764, 3767, 3770, 3773, 3776,
				3779, 3783, 3786, 3789, 3792, 3795, 3798, 3801, 3804, 3807, 3810, 3813, 3816, 3819, 3822, 3825, 3828, 3831, 3834, 3837, 3840, 3843, 3846, 3849, 3852, 3855, 3858, 3861, 3864, 3867,
				3870, 3873, 3876, 3879, 3882, 3885, 3888, 3891, 3894, 3897, 3900, 3902, 3905, 3908, 3911, 3914, 3917, 3920, 3923, 3926, 3929, 3932, 3935, 3938, 3941, 3944, 3947, 3950, 3953, 3955,
				3958, 3961, 3964, 3967, 3970, 3973, 3976, 3979, 3982, 3985, 3988, 3991, 3993, 3996, 3999, 4002, 4005, 4008, 4011, 4014, 4017, 4020, 4022, 4025, 4028, 4031, 4034, 4037, 4040, 4043,
				4046, 4048, 4051, 4054, 4057, 4060, 4063, 4066, 4068, 4071, 4074, 4077, 4080, 4083, 4086, 4089, 4091, 4094, 4097, 4100, 4103, 4106, 4108, 4111, 4114, 4117, 4120, 4123, 4125, 4128,
				4131, 4134, 4137, 4140, 4142, 4145, 4148, 4151, 4154, 4157, 4159, 4162, 4165, 4168, 4171, 4173, 4176, 4179, 4182, 4185, 4187, 4190, 4193, 4196, 4199, 4201, 4204, 4207, 4210, 4213,
				4215, 4218, 4221, 4224, 4226, 4229, 4232, 4235, 4238, 4240, 4243, 4246, 4249, 4251, 4254, 4257, 4260, 4262, 4265, 4268, 4271, 4274, 4276, 4279, 4282, 4285, 4287, 4290, 4293, 4296,
				4298, 4301, 4304, 4306, 4309, 4312, 4315, 4317, 4320, 4323, 4326, 4328, 4331, 4334, 4337, 4339, 4342, 4345, 4347, 4350, 4353, 4356, 4358, 4361, 4364, 4366, 4369, 4372, 4374, 4377,
				4380, 4383, 4385, 4388, 4391, 4393, 4396, 4399, 4401, 4404, 4407, 4410, 4412, 4415, 4418, 4420, 4423, 4426, 4428, 4431, 4434, 4436, 4439, 4442, 4444, 4447, 4450, 4452, 4455, 4458,
				4460, 4463, 4466, 4468, 4471, 4474, 4476, 4479, 4482, 4484, 4487, 4490, 4492, 4495, 4497, 4500, 4503, 4505, 4508, 4511, 4513, 4516, 4519, 4521, 4524, 4526, 4529, 4532, 4534, 4537,
				4540, 4542, 4545, 4548, 4550, 4553, 4555, 4558, 4561, 4563, 4566, 4568, 4571, 4574, 4576, 4579, 4581, 4584, 4587, 4589, 4592, 4594, 4597, 4600, 4602, 4605, 4607, 4610, 4613, 4615,
				4618, 4620, 4623, 4626, 4628, 4631, 4633, 4636, 4639, 4641, 4644, 4646, 4649, 4651, 4654, 4657, 4659, 4662, 4664, 4667, 4669, 4672, 4675, 4677, 4680, 4682, 4685, 4687, 4690, 4692,
				4695, 4698, 4700, 4703, 4705, 4708, 4710, 4713, 4715, 4718, 4720, 4723, 4726, 4728, 4731, 4733, 4736, 4738, 4741, 4743, 4746, 4748, 4751, 4753, 4756, 4759, 4761, 4764, 4766, 4769,
				4771, 4774, 4776, 4779, 4781, 4784, 4786, 4789, 4791, 4794, 4796, 4799, 4801, 4804, 4806, 4809, 4811, 4814, 4816, 4819, 4821, 4824, 4826, 4829, 4831, 4834, 4836, 4839, 4841, 4844,
				4846, 4849, 4851, 4854, 4856, 4859, 4861, 4864, 4866, 4869, 4871, 4874, 4876, 4879, 4881, 4884, 4886, 4889, 4891, 4893, 4896, 4898, 4901, 4903, 4906, 4908, 4911, 4913, 4916, 4918,
				4921, 4923, 4926, 4928, 4930, 4933, 4935, 4938, 4940, 4943, 4945, 4948, 4950, 4953, 4955, 4957, 4960, 4962, 4965, 4967, 4970, 4972, 4975, 4977, 4979, 4982, 4984, 4987, 4989, 4992,
				4994, 4996, 4999, 5001, 5004, 5006, 5009, 5011, 5013, 5016, 5018, 5021, 5023, 5026, 5028, 5030, 5033, 5035, 5038, 5040, 5042, 5045, 5047, 5050, 5052, 5055, 5057, 5059, 5062, 5064,
				5067, 5069, 5071, 5074, 5076, 5079, 5081, 5083, 5086, 5088, 5091, 5093, 5095, 5098, 5100, 5102, 5105, 5107, 5110, 5112, 5114, 5117, 5119, 5122, 5124, 5126, 5129, 5131, 5133, 5136
			};

		public static int GetLuckChance( Mobile from )
		{
			if ( !Core.AOS )
				return 0;

			int luck = from.Luck;

			if ( luck > m_LuckTable.Length )
				luck = m_LuckTable.Length;

			--luck;

			if ( luck < 0 )
				return 0;

			return m_LuckTable[luck];
		}

		public static int GetLuckChanceForKiller( Mobile dead )
		{
			ArrayList list = BaseCreature.GetLootingRights( dead.DamageEntries, dead.HitsMax );

			DamageStore highest = null;

			for ( int i = 0; i < list.Count; ++i )
			{
				DamageStore ds = (DamageStore)list[i];

				if ( ds.m_HasRight && (highest == null || ds.m_Damage > highest.m_Damage) )
					highest = ds;
			}

			if ( highest == null )
				return 0;

			return GetLuckChance( highest.m_Mobile );
		}

		public static bool CheckLuck( int chance )
		{
			return ( chance > Utility.Random( 10000 ) );
		}

		private LootPackEntry[] m_Entries;

		public LootPack( LootPackEntry[] entries )
		{
			m_Entries = entries;
		}

		public void Generate( Mobile from, Container cont, bool spawning, int luckChance )
		{
			if ( cont == null )
				return;

			bool checkLuck = Core.AOS;

			for ( int i = 0; i < m_Entries.Length; ++i )
			{
				LootPackEntry entry = m_Entries[i];

				bool shouldAdd = ( entry.Chance > Utility.Random( 10000 ) );

				if ( !shouldAdd && checkLuck )
				{
					checkLuck = false;

					if ( LootPack.CheckLuck( luckChance ) )
						shouldAdd = ( entry.Chance > Utility.Random( 10000 ) );
				}

				if ( !shouldAdd )
					continue;

				Item item = entry.Construct( from, luckChance, spawning );

				if ( item != null )
				{
					if ( !item.Stackable || !cont.TryDropItem( from, item, false ) )
						cont.DropItem( item );
				}
			}
		}

		public static readonly LootPackItem[] Gold = new LootPackItem[]
			{
				new LootPackItem( typeof( Gold ), 1 )
			};

		public static readonly LootPackItem[] Instruments = new LootPackItem[]
			{
				new LootPackItem( typeof( BaseInstrument ), 1 )
			};

		public static readonly LootPackItem[] MagicItems = new LootPackItem[]
			{
				new LootPackItem( typeof( BaseJewel ), 1 ),
				new LootPackItem( typeof( BaseArmor ), 4 ),
				new LootPackItem( typeof( BaseWeapon ), 4 ),
				new LootPackItem( typeof( BaseShield ), 1 )
			};

		public static readonly LootPackItem[] LowScrollItems = new LootPackItem[]
			{
				new LootPackItem( typeof( ClumsyScroll ), 1 )
			};

		public static readonly LootPackItem[] MedScrollItems = new LootPackItem[]
			{
				new LootPackItem( typeof( ArchCureScroll ), 1 )
			};

		public static readonly LootPackItem[] HighScrollItems = new LootPackItem[]
			{
				new LootPackItem( typeof( SummonAirElementalScroll ), 1 )
			};

		public static readonly LootPackItem[] GemItems = new LootPackItem[]
			{
				new LootPackItem( typeof( Amber ), 1 )
			};

		public static readonly LootPackItem[] PotionItems = new LootPackItem[]
			{
				new LootPackItem( typeof( AgilityPotion ), 1 ),
				new LootPackItem( typeof( StrengthPotion ), 1 ),
				new LootPackItem( typeof( RefreshPotion ), 1 ),
				new LootPackItem( typeof( LesserCurePotion ), 1 ),
				new LootPackItem( typeof( LesserHealPotion ), 1 ),
				new LootPackItem( typeof( LesserPoisonPotion ), 1 )
			};

		#region AOS definitions
		public static readonly LootPack AosPoor = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "1d10+10" ),
				new LootPackEntry( false, MagicItems,	  0.02, 1, 5, 0, 90 ),
				new LootPackEntry( false, Instruments,	  0.02, 1 )
			} );

		public static readonly LootPack AosMeager = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "3d10+20" ),
				new LootPackEntry( false, MagicItems,	  1.00, 1, 2, 0, 10 ),
				new LootPackEntry( false, MagicItems,	  0.20, 1, 5, 0, 90 ),
				new LootPackEntry( false, Instruments,	  0.10, 1 )
			} );

		public static readonly LootPack AosAverage = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "5d10+50" ),
				new LootPackEntry( false, MagicItems,	  5.00, 1, 4, 0, 20 ),
				new LootPackEntry( false, MagicItems,	  2.00, 1, 3, 0, 50 ),
				new LootPackEntry( false, MagicItems,	  0.50, 1, 5, 0, 90 ),
				new LootPackEntry( false, Instruments,	  0.40, 1 )
			} );

		public static readonly LootPack AosRich = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "10d10+150" ),
				new LootPackEntry( false, MagicItems,	 20.00, 1, 4, 0, 40 ),
				new LootPackEntry( false, MagicItems,	 10.00, 1, 5, 0, 60 ),
				new LootPackEntry( false, MagicItems,	  1.00, 1, 5, 0, 90 ),
				new LootPackEntry( false, Instruments,	  1.00, 1 )
			} );

		public static readonly LootPack AosFilthyRich = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "2d100+200" ),
				new LootPackEntry( false, MagicItems,	 33.00, 1, 4, 0, 50 ),
				new LootPackEntry( false, MagicItems,	 33.00, 1, 4, 0, 60 ),
				new LootPackEntry( false, MagicItems,	 20.00, 1, 5, 0, 75 ),
				new LootPackEntry( false, MagicItems,	  5.00, 1, 5, 0, 100 ),
				new LootPackEntry( false, Instruments,	  2.00, 1 )
			} );

		public static readonly LootPack AosUltraRich = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "5d100+500" ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 25, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 25, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 25, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 25, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 25, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 35, 100 ),
				new LootPackEntry( false, Instruments,	  2.00, 1 )
			} );

		public static readonly LootPack AosSuperBoss = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "5d100+500" ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 25, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 25, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 25, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 25, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 33, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 33, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 33, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 33, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 50, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 5, 50, 100 ),
				new LootPackEntry( false, Instruments,	  2.00, 1 )
			} );
		#endregion

		#region Pre-AOS definitions
		public static readonly LootPack OldPoor = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "1d25" ),
				new LootPackEntry( false, Instruments,	  0.02, 1 )
			} );

		public static readonly LootPack OldMeager = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "5d10+25" ),
				new LootPackEntry( false, Instruments,	  0.10, 1 ),
				new LootPackEntry( false, MagicItems,	  1.00, 1, 1, 0, 60 ),
				new LootPackEntry( false, MagicItems,	  0.20, 1, 1, 10, 70 )
			} );

		public static readonly LootPack OldAverage = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "10d10+50" ),
				new LootPackEntry( false, Instruments,	  0.40, 1 ),
				new LootPackEntry( false, MagicItems,	  5.00, 1, 1, 20, 80 ),
				new LootPackEntry( false, MagicItems,	  2.00, 1, 1, 30, 90 ),
				new LootPackEntry( false, MagicItems,	  0.50, 1, 1, 40, 100 )
			} );

		public static readonly LootPack OldRich = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "10d10+250" ),
				new LootPackEntry( false, Instruments,	  1.00, 1 ),
				new LootPackEntry( false, MagicItems,	 20.00, 1, 1, 60, 100 ),
				new LootPackEntry( false, MagicItems,	 10.00, 1, 1, 65, 100 ),
				new LootPackEntry( false, MagicItems,	  1.00, 1, 1, 70, 100 )
			} );

		public static readonly LootPack OldFilthyRich = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "2d125+400" ),
				new LootPackEntry( false, Instruments,	  2.00, 1 ),
				new LootPackEntry( false, MagicItems,	 33.00, 1, 1, 50, 100 ),
				new LootPackEntry( false, MagicItems,	 33.00, 1, 1, 60, 100 ),
				new LootPackEntry( false, MagicItems,	 20.00, 1, 1, 70, 100 ),
				new LootPackEntry( false, MagicItems,	  5.00, 1, 1, 80, 100 )
			} );

		public static readonly LootPack OldUltraRich = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "5d100+500" ),
				new LootPackEntry( false, Instruments,	  2.00, 1 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 40, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 40, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 50, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 50, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 60, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 60, 100 )
			} );

		public static readonly LootPack OldSuperBoss = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry(  true, Gold,			100.00, "5d100+500" ),
				new LootPackEntry( false, Instruments,	  2.00, 1 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 40, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 40, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 40, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 50, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 50, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 50, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 60, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 60, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 60, 100 ),
				new LootPackEntry( false, MagicItems,	100.00, 1, 1, 70, 100 )
			} );
		#endregion

		#region Generic accessors
		public static LootPack Poor{ get{ return Core.AOS ? AosPoor : OldPoor; } }
		public static LootPack Meager{ get{ return Core.AOS ? AosMeager : OldMeager; } }
		public static LootPack Average{ get{ return Core.AOS ? AosAverage : OldAverage; } }
		public static LootPack Rich{ get{ return Core.AOS ? AosRich : OldRich; } }
		public static LootPack FilthyRich{ get{ return Core.AOS ? AosFilthyRich : OldFilthyRich; } }
		public static LootPack UltraRich{ get{ return Core.AOS ? AosUltraRich : OldUltraRich; } }
		public static LootPack SuperBoss{ get{ return Core.AOS ? AosSuperBoss : OldSuperBoss; } }
		#endregion

		public static readonly LootPack LowScrolls = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry( false, LowScrollItems,	100.00, 1 )
			} );

		public static readonly LootPack MedScrolls = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry( false, MedScrollItems,	100.00, 1 )
			} );

		public static readonly LootPack HighScrolls = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry( false, HighScrollItems,	100.00, 1 )
			} );

		public static readonly LootPack Gems = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry( false, GemItems,			100.00, 1 )
			} );

		public static readonly LootPack Potions = new LootPack( new LootPackEntry[]
			{
				new LootPackEntry( false, PotionItems,		100.00, 1 )
			} );
	}

	public class LootPackEntry
	{
		private int m_Chance;
		private LootPackDice m_Quantity;

		private int m_MaxProps, m_MinIntensity, m_MaxIntensity;

		private bool m_AtSpawnTime;

		private LootPackItem[] m_Items;

		public int Chance
		{
			get{ return m_Chance; }
			set{ m_Chance = value; }
		}

		public LootPackDice Quantity
		{
			get{ return m_Quantity; }
			set{ m_Quantity = value; }
		}

		public int MaxProps
		{
			get{ return m_MaxProps; }
			set{ m_MaxProps = value; }
		}

		public int MinIntensity
		{
			get{ return m_MinIntensity; }
			set{ m_MinIntensity = value; }
		}

		public int MaxIntensity
		{
			get{ return m_MaxIntensity; }
			set{ m_MaxIntensity = value; }
		}

		public LootPackItem[] Items
		{
			get{ return m_Items; }
			set{ m_Items = value; }
		}

		public Item Construct( Mobile from, int luckChance, bool spawning )
		{
			if ( m_AtSpawnTime != spawning )
				return null;

			int totalChance = 0;

			for ( int i = 0; i < m_Items.Length; ++i )
				totalChance += m_Items[i].Chance;

			int rnd = Utility.Random( totalChance );

			for ( int i = 0; i < m_Items.Length; ++i )
			{
				LootPackItem item = m_Items[i];

				if ( rnd < item.Chance )
					return Mutate( from, luckChance, item.Construct() );

				rnd -= item.Chance;
			}

			return null;
		}

		private int GetRandomOldBonus()
		{
			int rnd = Utility.Random( m_MinIntensity, m_MaxIntensity - m_MinIntensity );

			if ( 50 > rnd )
				return 1;
			else
				rnd -= 50;

			if ( 25 > rnd )
				return 2;
			else
				rnd -= 25;

			if ( 14 > rnd )
				return 3;
			else
				rnd -= 14;

			if ( 8 > rnd )
				return 4;

			return 5;
		}

		public Item Mutate( Mobile from, int luckChance, Item item )
		{
			if ( item != null )
			{
				if ( item is BaseWeapon && 1 > Utility.Random( 100 ) )
				{
					item.Delete();
					item = new FireHorn();
					return item;
				}

				if ( item is BaseWeapon || item is BaseArmor || item is BaseJewel )
				{
					if ( Core.AOS )
					{
						int bonusProps = GetBonusProperties();
						int min = m_MinIntensity;
						int max = m_MaxIntensity;

						if ( bonusProps < m_MaxProps && LootPack.CheckLuck( luckChance ) )
							++bonusProps;

						int props = 1 + bonusProps;

						if ( item is BaseWeapon )
							BaseRunicTool.ApplyAttributesTo( (BaseWeapon)item, false, luckChance, props, m_MinIntensity, m_MaxIntensity );
						else if ( item is BaseArmor )
							BaseRunicTool.ApplyAttributesTo( (BaseArmor)item, false, luckChance, props, m_MinIntensity, m_MaxIntensity );
						else if ( item is BaseJewel )
							BaseRunicTool.ApplyAttributesTo( (BaseJewel)item, false, luckChance, props, m_MinIntensity, m_MaxIntensity );
					}
					else // not aos
					{
						if ( item is BaseWeapon )
						{
							BaseWeapon weapon = (BaseWeapon)item;

							if ( 80 > Utility.Random( 100 ) )
								weapon.AccuracyLevel = (WeaponAccuracyLevel)GetRandomOldBonus();

							if ( 60 > Utility.Random( 100 ) )
								weapon.DamageLevel = (WeaponDamageLevel)GetRandomOldBonus();

							if ( 40 > Utility.Random( 100 ) )
								weapon.DurabilityLevel = (WeaponDurabilityLevel)GetRandomOldBonus();

							if ( 5 > Utility.Random( 100 ) )
								weapon.Slayer = SlayerName.Silver;

							if ( weapon.AccuracyLevel == 0 && weapon.DamageLevel == 0 && weapon.DurabilityLevel == 0 && weapon.Slayer == SlayerName.None && 5 > Utility.Random( 100 ) )
								weapon.Slayer = SlayerGroup.GetLootSlayerType( from.GetType() );
						}
						else if ( item is BaseArmor )
						{
							BaseArmor armor = (BaseArmor)item;

							if ( 80 > Utility.Random( 100 ) )
								armor.ProtectionLevel = (ArmorProtectionLevel)GetRandomOldBonus();

							if ( 40 > Utility.Random( 100 ) )
								armor.Durability = (ArmorDurabilityLevel)GetRandomOldBonus();
						}
					}
				}
				else if ( item is BaseInstrument )
				{
					SlayerName slayer = SlayerName.None;

					if ( Core.AOS )
						slayer = BaseRunicTool.GetRandomSlayer();
					else
						slayer = SlayerGroup.GetLootSlayerType( from.GetType() );

					if ( slayer == SlayerName.None )
					{
						item.Delete();
						return null;
					}

					BaseInstrument instr = (BaseInstrument)item;

					instr.Quality = InstrumentQuality.Regular;
					instr.Slayer = slayer;
				}

				if ( item.Stackable )
					item.Amount = m_Quantity.Roll();
			}

			return item;
		}

		public LootPackEntry( bool atSpawnTime, LootPackItem[] items, double chance, string quantity ) : this( atSpawnTime, items, chance, new LootPackDice( quantity ), 0, 0, 0 )
		{
		}

		public LootPackEntry( bool atSpawnTime, LootPackItem[] items, double chance, int quantity ) : this( atSpawnTime, items, chance, new LootPackDice( 0, 0, quantity ), 0, 0, 0 )
		{
		}

		public LootPackEntry( bool atSpawnTime, LootPackItem[] items, double chance, string quantity, int maxProps, int minIntensity, int maxIntensity ) : this( atSpawnTime, items, chance, new LootPackDice( quantity ), maxProps, minIntensity, maxIntensity )
		{
		}

		public LootPackEntry( bool atSpawnTime, LootPackItem[] items, double chance, int quantity, int maxProps, int minIntensity, int maxIntensity ) : this( atSpawnTime, items, chance, new LootPackDice( 0, 0, quantity ), maxProps, minIntensity, maxIntensity )
		{
		}

		public LootPackEntry( bool atSpawnTime, LootPackItem[] items, double chance, LootPackDice quantity, int maxProps, int minIntensity, int maxIntensity )
		{
			m_AtSpawnTime = atSpawnTime;
			m_Items = items;
			m_Chance = (int)(100 * chance);
			m_Quantity = quantity;
			m_MaxProps = maxProps;
			m_MinIntensity = minIntensity;
			m_MaxIntensity = maxIntensity;
		}

		public int GetBonusProperties()
		{
			int p0=0, p1=0, p2=0, p3=0, p4=0, p5=0;

			switch ( m_MaxProps )
			{
				case 1: p0= 3; p1= 1; break;
				case 2: p0= 6; p1= 3; p2= 1; break;
				case 3: p0=10; p1= 6; p2= 3; p3= 1; break;
				case 4: p0=16; p1=12; p2= 6; p3= 5; p4=1; break;
				case 5: p0=30; p1=25; p2=20; p3=15; p4=9; p5=1; break;
			}

			int pc = p0+p1+p2+p3+p4+p5;

			int rnd = Utility.Random( pc );

			if ( rnd < p5 )
				return 5;
			else
				rnd -= p5;

			if ( rnd < p4 )
				return 4;
			else
				rnd -= p4;

			if ( rnd < p3 )
				return 3;
			else
				rnd -= p3;

			if ( rnd < p2 )
				return 2;
			else
				rnd -= p2;

			if ( rnd < p1 )
				return 1;

			return 0;
		}
	}

	public class LootPackItem
	{
		private Type m_Type;
		private int m_Chance;

		public Type Type
		{
			get{ return m_Type; }
			set{ m_Type = value; }
		}

		public int Chance
		{
			get{ return m_Chance; }
			set{ m_Chance = value; }
		}

		private static Type[]   m_BlankTypes = new Type[]{ typeof( BlankScroll ) };
		private static Type[][] m_NecroTypes = new Type[][]
			{
				new Type[] // low
				{
					typeof( AnimateDeadScroll ),		typeof( BloodOathScroll ),		typeof( CorpseSkinScroll ),	typeof( CurseWeaponScroll ),
					typeof( EvilOmenScroll ),			typeof( HorrificBeastScroll ),	typeof( MindRotScroll ),	typeof( PainSpikeScroll ),
					typeof( SummonFamiliarScroll ),		typeof( WraithFormScroll )
				},
				new Type[] // med
				{
					typeof( LichFormScroll ),			typeof( PoisonStrikeScroll ),	typeof( StrangleScroll ),	typeof( WitherScroll )
				},
				new Type[] // high
				{
					typeof( VengefulSpiritScroll ),		typeof( VampiricEmbraceScroll )
				}
			};

		public static Item RandomScroll( int index, int minCircle, int maxCircle )
		{
			--minCircle;
			--maxCircle;

			int scrollCount = ((maxCircle - minCircle) + 1) * 8;

			if ( index == 0 )
				scrollCount += m_BlankTypes.Length;

			if ( Core.AOS )
				scrollCount += m_NecroTypes[index].Length;

			int rnd = Utility.Random( scrollCount );

			if ( index == 0 && rnd < m_BlankTypes.Length )
				return Loot.Construct( m_BlankTypes );
			else if ( index == 0 )
				rnd -= m_BlankTypes.Length;

			if ( Core.AOS && rnd < m_NecroTypes.Length )
				return Loot.Construct( m_NecroTypes[index] );
			else if ( Core.AOS )
				rnd -= m_NecroTypes[index].Length;

			return Loot.RandomScroll( minCircle * 8, (maxCircle * 8) + 7, SpellbookType.Regular );
		}

		public Item Construct()
		{
			try
			{
				Item item;

				if ( m_Type == typeof( BaseWeapon ) )
					item = Loot.RandomWeapon();
				else if ( m_Type == typeof( BaseArmor ) )
					item = Loot.RandomArmor();
				else if ( m_Type == typeof( BaseShield ) )
					item = Loot.RandomShield();
				else if ( m_Type == typeof( BaseJewel ) )
					item = Core.AOS ? Loot.RandomJewelry() : Loot.RandomArmorOrShieldOrWeapon();
				else if ( m_Type == typeof( BaseInstrument ) )
					item = Loot.RandomInstrument();
				else if ( m_Type == typeof( Amber ) ) // gem
					item = Loot.RandomGem();
				else if ( m_Type == typeof( ClumsyScroll ) ) // low scroll
					item = RandomScroll( 0, 1, 3 );
				else if ( m_Type == typeof( ArchCureScroll ) ) // med scroll
					item = RandomScroll( 1, 4, 7 );
				else if ( m_Type == typeof( SummonAirElementalScroll ) ) // high scroll
					item = RandomScroll( 2, 8, 8 );
				else
					item = Activator.CreateInstance( m_Type ) as Item;

				return item;
			}
			catch
			{
			}

			return null;
		}

		public LootPackItem( Type type, int chance )
		{
			m_Type = type;
			m_Chance = chance;
		}
	}

	public class LootPackDice
	{
		private int m_Count, m_Sides, m_Bonus;

		public int Count
		{
			get{ return m_Count; }
			set{ m_Count = value; }
		}

		public int Sides
		{
			get{ return m_Sides; }
			set{ m_Sides = value; }
		}

		public int Bonus
		{
			get{ return m_Bonus; }
			set{ m_Bonus = value; }
		}

		public int Roll()
		{
			int v = m_Bonus;

			for ( int i = 0; i < m_Count; ++i )
				v += Utility.Random( 1, m_Sides );

			return v;
		}

		public LootPackDice( string str )
		{
			int start = 0;
			int index = str.IndexOf( 'd', start );

			if ( index < start )
				return;

			m_Count = Utility.ToInt32( str.Substring( start, index-start ) );

			bool negative;

			start = index + 1;
			index = str.IndexOf( '+', start );

			if ( negative = (index < start) )
				index = str.IndexOf( '-', start );

			if ( index < start )
				index = str.Length;

			m_Sides = Utility.ToInt32( str.Substring( start, index-start ) );

			if ( index == str.Length )
				return;

			start = index + 1;
			index = str.Length;

			m_Bonus = Utility.ToInt32( str.Substring( start, index-start ) );

			if ( negative )
				m_Bonus *= -1;
		}

		public LootPackDice( int count, int sides, int bonus )
		{
			m_Count = count;
			m_Sides = sides;
			m_Bonus = bonus;
		}
	}
}