using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


public class SearchFilterImpExpWindow : EditorWindow 
{
    List<Data> m_exportTargets = new List<Data>(10);
    List<Data> m_importTargets = new List<Data>(10);
    int m_tabIndex = 0;

	/// <summary>
    /// .
    /// </summary>
	[MenuItem( "Tools/Open SearchFilterImpExpWindow" )]
    private static void Init()
    {
        GetWindow( typeof( SearchFilterImpExpWindow ), false, "SearchFilterImpExp", true );
    }

	/// <summary>
    /// .
    /// </summary>
    private void OnGUI()
    {
        m_tabIndex = GUILayout.Toolbar( m_tabIndex, new string[]{ "Export Settings", "Import Settings" } );
        switch( m_tabIndex )
        {
            case 0: // Export Settings.
            {
                if( GUILayout.Button( "Update current filters" )){
                    UpdateFilters();
                }

                if( m_exportTargets.Count > 0 )
                {
                    foreach( var filter in m_exportTargets ){
                        filter.IsChecked = GUILayout.Toggle( filter.IsChecked, filter.Name );
                    }

                    if( GUILayout.Button( "Export" ))
                    {
                        List<object> targetFilters = new List<object>( 10 );
                        foreach( var data in m_exportTargets )
                        {
                            if( data.IsChecked ){
                                targetFilters.Add( data.Filter );
                            }
                        }

                        string filePath = EditorUtility.SaveFilePanel( "Export search filters as Binary", "", "sample.bin", "bin" );
                        if( filePath == null || filePath.Length <= 0 ){
                            return;
                        }
            
                        using( FileStream fs = new FileStream( filePath, FileMode.Create, FileAccess.Write ))
                        {
                            BinaryFormatter bf = new BinaryFormatter();
                            bf.Serialize( fs, targetFilters.ToArray());
                        }
                    }
                }
            }
            break;


            case 1: // Import Settings.
            {
                if( GUILayout.Button( "Import" ))
                {
                    string filePath = EditorUtility.OpenFilePanel( "Import search filters", "", "bin" );
                    if( filePath == null || filePath.Length <= 0 ){
                        return;
                    }

                    object[] tmpFilters;
                    using( FileStream fs = new FileStream( filePath, FileMode.Open, FileAccess.Read ))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        tmpFilters = (bf.Deserialize( fs ) as object[]);
                    }

                    m_importTargets.Clear();

                    foreach( var filter in tmpFilters )
                    {
                        string filterName = GetString( "m_Name", filter );
                        m_importTargets.Add( new Data( filter, filterName, false ));
                    }
                }

                if( m_importTargets.Count > 0 )
                {
                    foreach( var filter in m_importTargets ){
                        filter.IsChecked = GUILayout.Toggle( filter.IsChecked, filter.Name );
                    }

                    if( GUILayout.Button( "Apply" )){
                        Apply();
                    }
                }
            }
            break;
        }
	}


    /// <summary>
    /// .
    /// </summary>
    private void UpdateFilters()
    {
        var savedSearchFilters = LoadSavedSearchFilters();
        if( savedSearchFilters == null){
            return;
        }

        m_exportTargets.Clear();

        var searchFilter = savedSearchFilters[ 0 ];
        var saveFilters = (GetObject( "m_SavedFilters", searchFilter ) as IEnumerable);
        foreach( var filter in saveFilters )
        {
            // Favoriteはルートディレクトリなので無視する.
            string filterName = GetString( "m_Name", filter );
            if( filterName == "Favorites" ){
                continue;
            }
            // 念のため階層でも見ておく.
            int depth = GetFieldValueInt( "m_Depth", filter );
            if( depth < 1 ){
                continue;
            }

            m_exportTargets.Add( new Data( filter, filterName, false ));
        } 
    }


    /// <summary>
    /// .
    /// </summary>
    private void Apply()
    {
        if( m_importTargets.Count <= 0 ){
            return;
        }

        var savedSearchFilters = LoadSavedSearchFilters();
        if( savedSearchFilters == null){
            Debug.LogError( "Not found SavedSearchFilters!!" );
            return;
        }

        var searchFilter = savedSearchFilters[ 0 ];
        System.Type t = searchFilter.GetType();
        MethodInfo methodInfo = t.GetMethod( "AddSavedFilter" );

        foreach( var data in m_importTargets )
        {
            if( data.IsChecked )
            {
                object filter = data.Filter;
                string filterName = GetString( "m_Name", filter );
                float previewSize = GetFloat( "m_PreviewSize", filter );
                object targetFilter = GetObject( "m_Filter", filter );

                object[] args = new object[]{ filterName, targetFilter, previewSize };
                methodInfo.Invoke( searchFilter, args );
            }
        }

        EditorApplication.RepaintProjectWindow();
    }


    /// <summary>
    /// .
    /// </summary>
    private Object[] LoadSavedSearchFilters()
    {
        var asm = Assembly.Load( "UnityEditor.dll" );
        var type = asm.GetType( "UnityEditor.SavedSearchFilters" );
        var savedSearchFilters = Resources.FindObjectsOfTypeAll( type );
        return savedSearchFilters;
    } 


    private int GetFieldValueInt( string name, object obj )
    { 
        return System.Convert.ToInt32( GetObject( name, obj ));
    }
    private float GetFloat( string name, object obj )
    { 
        return System.Convert.ToSingle( GetObject( name, obj )); 
    }
    private string GetString( string name, object obj )
    { 
        return System.Convert.ToString( GetObject( name, obj ));
    }

    /// <summary>
    /// .
    /// </summary>
    private object GetObject( string fieldName, object obj )
    {
        if( obj == null ){
            return null;
        }

        BindingFlags flag = (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        System.Type type = obj.GetType();
        FieldInfo fi = type.GetField( fieldName, flag );
        if( fi == null ){
            fi = type.BaseType.GetField( fieldName, flag );
        }
        if( fi == null ){
            return null;
        }
        return fi.GetValue( obj );
    }
    

    class Data
    {
        public Data( object filter, string name, bool isChecked )
        {
            Filter = filter;
            Name = name;
            IsChecked = isChecked;
        }

        public object Filter{ get; set; }
        public string Name{ get; set; }
        public bool IsChecked{ get; set; }
    }
}
