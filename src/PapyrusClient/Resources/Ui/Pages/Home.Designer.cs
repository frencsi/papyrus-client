﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PapyrusClient.Resources.Ui.Pages {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Home {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Home() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("PapyrusClient.Resources.Ui.Pages.Home", typeof(Home).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dashboard.
        /// </summary>
        internal static string BUTTON_TEXT_DASHBOARD {
            get {
                return ResourceManager.GetString("BUTTON_TEXT_DASHBOARD", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Process.
        /// </summary>
        internal static string BUTTON_TEXT_PROCESS {
            get {
                return ResourceManager.GetString("BUTTON_TEXT_PROCESS", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Remove.
        /// </summary>
        internal static string BUTTON_TEXT_REMOVE {
            get {
                return ResourceManager.GetString("BUTTON_TEXT_REMOVE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Work schedule.
        /// </summary>
        internal static string DATAGRID_COLUMN_TITLE_SCHEDULES {
            get {
                return ResourceManager.GetString("DATAGRID_COLUMN_TITLE_SCHEDULES", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The browser window may become momentarily unresponsive during the operation..
        /// </summary>
        internal static string MESSAGEBAR_BODY_PROCESSING_SELECTED_SCHEDULES {
            get {
                return ResourceManager.GetString("MESSAGEBAR_BODY_PROCESSING_SELECTED_SCHEDULES", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Processing selected files.
        /// </summary>
        internal static string MESSAGEBAR_TITLE_PROCESSING_SELECTED_SCHEDULES {
            get {
                return ResourceManager.GetString("MESSAGEBAR_TITLE_PROCESSING_SELECTED_SCHEDULES", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error occurred while processing work schedules, please see the console for details..
        /// </summary>
        internal static string TOAST_TEXT_ERROR_DURING_PROCESSING_SCHEDULES {
            get {
                return ResourceManager.GetString("TOAST_TEXT_ERROR_DURING_PROCESSING_SCHEDULES", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You can only process work schedules with no issues..
        /// </summary>
        internal static string TOAST_TEXT_INVALID_SCHEDULES_SELECTED_TO_PROCESS {
            get {
                return ResourceManager.GetString("TOAST_TEXT_INVALID_SCHEDULES_SELECTED_TO_PROCESS", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please select at least one work schedule..
        /// </summary>
        internal static string TOAST_TEXT_NO_SCHEDULES_SELECTED_TO_PROCESS {
            get {
                return ResourceManager.GetString("TOAST_TEXT_NO_SCHEDULES_SELECTED_TO_PROCESS", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please select at least one work schedule..
        /// </summary>
        internal static string TOAST_TEXT_NO_SCHEDULES_SELECTED_TO_REMOVE {
            get {
                return ResourceManager.GetString("TOAST_TEXT_NO_SCHEDULES_SELECTED_TO_REMOVE", resourceCulture);
            }
        }
    }
}
