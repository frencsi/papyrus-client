﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PapyrusClient.Resources.Services.WorkScheduleValidator {
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
    internal class WorkScheduleValidator {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal WorkScheduleValidator() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("PapyrusClient.Resources.Services.WorkScheduleValidator.WorkScheduleValidator", typeof(WorkScheduleValidator).Assembly);
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
        ///   Looks up a localized string similar to There is no options set for this work schedule.
        ///
        ///Please provide the options..
        /// </summary>
        internal static string NoOptionsSet {
            get {
                return ResourceManager.GetString("NoOptionsSet", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A required &apos;end&apos; continuation marker is missing for the employee &apos;{0}&apos; on &apos;{1}&apos;. 
        ///            
        ///This marker is needed because the shift on &apos;{2}&apos; for the same employee starts with a continuation marker. 
        ///
        ///Please review the following dates: &apos;{2}&apos; and &apos;{1}&apos;..
        /// </summary>
        internal static string PreviousShiftMissingEndContinuationMarker {
            get {
                return ResourceManager.GetString("PreviousShiftMissingEndContinuationMarker", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Multiple end continuation markers were found for employee &apos;{0}&apos; on &apos;{1}&apos;. 
        ///
        ///Please review the following dates: &apos;{2}&apos; and &apos;{1}&apos;..
        /// </summary>
        internal static string PreviousShiftMultipleEndContinuationMarkerFound {
            get {
                return ResourceManager.GetString("PreviousShiftMultipleEndContinuationMarkerFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Shift duration can&apos;t be negative or zero. 
        ///
        ///Details: {0}..
        /// </summary>
        internal static string ShiftDurationNegativeOrZero {
            get {
                return ResourceManager.GetString("ShiftDurationNegativeOrZero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The combined total shift time &apos;{0}&apos; exceeded the allowed maximum of &apos;{1}&apos;.
        ///
        ///Please review the following date: &apos;{2}&apos;..
        /// </summary>
        internal static string ShiftExceedsMaxDurationCombinedPerDay {
            get {
                return ResourceManager.GetString("ShiftExceedsMaxDurationCombinedPerDay", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Employee &apos;{0}&apos; logged &apos;{1}&apos; time, which exceeds the maximum allowed limit of &apos;{2}&apos;. 
        ///
        ///Please review the following date: &apos;{3}&apos;..
        /// </summary>
        internal static string ShiftExceedsMaxDurationPerEmployeePerDay {
            get {
                return ResourceManager.GetString("ShiftExceedsMaxDurationPerEmployeePerDay", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A gap was detected between shifts on &apos;{0}&apos;.
        ///
        ///Details: 
        ///    - Previous Shift: Employee &apos;{1}&apos; from &apos;{2}&apos; to &apos;{3}&apos; 
        ///    - Next Shift: Employee &apos;{4}&apos; from &apos;{5}&apos; to &apos;{6}&apos; 
        ///    - Gap: &apos;{7}&apos;
        ///
        ///Please ensure that no gaps exist between shifts..
        /// </summary>
        internal static string ShiftGapDetected {
            get {
                return ResourceManager.GetString("ShiftGapDetected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Shift data is missing for the following days: [{0}]..
        /// </summary>
        internal static string ShiftMissingRequiredDays {
            get {
                return ResourceManager.GetString("ShiftMissingRequiredDays", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There is an overlap in shifts on &apos;{0}&apos;. 
        ///
        ///Details: 
        ///    - Previous Shift: Employee &apos;{1}&apos; from &apos;{2}&apos; to &apos;{3}&apos; 
        ///    - Next Shift: Employee &apos;{4}&apos; from &apos;{5}&apos; to &apos;{6}&apos; 
        ///
        ///Please review the timings for overlapping shifts..
        /// </summary>
        internal static string ShiftOverlapDetected {
            get {
                return ResourceManager.GetString("ShiftOverlapDetected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to One or more shift dates are not in the header specified month &apos;{0}&apos;. 
        ///
        ///Please review the following date: &apos;{1}&apos;..
        /// </summary>
        internal static string ShiftsDateNotInSpecifiedMonth {
            get {
                return ResourceManager.GetString("ShiftsDateNotInSpecifiedMonth", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to One or more shift dates are not in the header specified year &apos;{0}&apos;. 
        ///            
        ///Please review the following date: &apos;{1}&apos;..
        /// </summary>
        internal static string ShiftsDateNotInSpecifiedYear {
            get {
                return ResourceManager.GetString("ShiftsDateNotInSpecifiedYear", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only the operator work type currently supports gap validation..
        /// </summary>
        internal static string WorTypeGapValidationNotSupported {
            get {
                return ResourceManager.GetString("WorTypeGapValidationNotSupported", resourceCulture);
            }
        }
    }
}
