using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UniEditorPrefsWindow
{
	/// <summary>
	/// EditorPrefs が保存しているすべてのキーと値を閲覧できるエディタ拡張
	/// </summary>
	internal sealed class EditorPrefsWindow : EditorWindow
	{
		//================================================================================
		// クラス
		//================================================================================
		/// <summary>
		/// キーと値の情報を管理するクラス
		/// </summary>
		private sealed class KeyValueData
		{
			public string Key   { get; }
			public string Value { get; }

			public KeyValueData( string key, string value )
			{
				Key   = key;
				Value = value;
			}

			public bool IsFilter( string searchText )
			{
				searchText = searchText.ToLower();

				return Key.ToLower().Contains( searchText ) ||
				       Value.ToLower().Contains( searchText );
			}

			public void Deconstruct( out string key, out string value ) => ( key, value ) = ( Key, Value );
		}
		
		//================================================================================
		// 変数
		//================================================================================
		private KeyValueData[] m_list;
		private Vector2        m_scrollPosition;
		private string         m_searchText = string.Empty;
		
		//================================================================================
		// 関数
		//================================================================================
		/// <summary>
		/// 有効になった時に呼び出されます
		/// </summary>
		private void OnEnable()
		{
			Refresh();
		}

		/// <summary>
		/// キーと値の情報を取得し直します
		/// </summary>
		private void Refresh()
		{
			m_list = GetEditorPrefsKeyValuePairAll()
					.OrderBy( x => x.Key )
					.ToArray()
				;
		}

		/// <summary>
		/// GUI を描画する時に呼び出されます
		/// </summary>
		private void OnGUI()
		{
			using ( new EditorGUILayout.HorizontalScope( EditorStyles.toolbar ) )
			{
				if ( GUILayout.Button( "Refresh", EditorStyles.toolbarButton ) )
				{
					Refresh();
					Repaint();
				}

				GUILayout.FlexibleSpace();

				m_searchText = EditorGUILayout.TextField
				(
					m_searchText,
					EditorStyles.toolbarSearchField,
					GUILayout.Width( 256 )
				);
			}

			using ( var scope = new EditorGUILayout.ScrollViewScope( m_scrollPosition ) )
			{
				var isSearch = !string.IsNullOrWhiteSpace( m_searchText );
				var list     = m_list.Where( x => !isSearch || x.IsFilter( m_searchText ) );

				foreach ( var (key, value) in list )
				{
					using ( new EditorGUILayout.HorizontalScope() )
					{
						EditorGUILayout.TextField( key, GUILayout.Width( 256 ) );
						EditorGUILayout.TextField( value );
					}
				}

				m_scrollPosition = scope.scrollPosition;
			}
		}
		
		//================================================================================
		// 関数(static)
		//================================================================================
		/// <summary>
		/// 開きます
		/// </summary>
		[MenuItem( "Window/" + nameof( UniEditorPrefsWindow ) )]
		private static void Open()
		{
			GetWindow<EditorPrefsWindow>( nameof( UniEditorPrefsWindow ) );
		}

		/// <summary>
		/// EditorPrefs が保存しているすべてのキーと値の情報を返します
		/// </summary>
		private static IEnumerable<KeyValueData> GetEditorPrefsKeyValuePairAll()
		{
			var name = @"Software\Unity Technologies\Unity Editor 5.x\";
			using ( var registryKey = Registry.CurrentUser.OpenSubKey( name, false ) )
			{
				foreach ( var valueName in registryKey.GetValueNames() )
				{
					var value = registryKey.GetValue( valueName );
					var key   = valueName.Split( new[] { "_h" }, StringSplitOptions.None )[ 0 ];

					if ( value is byte[] byteValue )
					{
						yield return new KeyValueData( key, Encoding.UTF8.GetString( byteValue ) );
					}
					else
					{
						yield return new KeyValueData( key, value.ToString() );
					}
				}
			}
		}
	}
}