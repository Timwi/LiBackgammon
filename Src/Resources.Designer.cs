﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LiBackgammon {
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
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("LiBackgammon.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] Css {
            get {
                object obj = ResourceManager.GetObject("Css", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] JQuery {
            get {
                object obj = ResourceManager.GetObject("JQuery", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to $(function ()
        ///{
        ///    // Keyboard shortcut handling
        ///    $(&apos;*[accesskey]&apos;).each(function ()
        ///    {
        ///        $(this).append($(&apos;&lt;span&gt;&apos;).addClass(&apos;shortcut&apos;).text($(this).attr(&apos;accesskey&apos;)));
        ///    });
        ///
        ///    $(document).keydown(function (e)
        ///    {
        ///        if (e.keyCode === 18)  // ALT key
        ///            $(document.body).addClass(&apos;show-shortcuts&apos;);
        ///    });
        ///
        ///    $(document).keyup(function (e)
        ///    {
        ///        if (e.keyCode === 18)  // ALT key
        ///            $(document.body).removeClass(&apos;show-shortcuts&apos;);
        ///    }) [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string Js {
            get {
                return ResourceManager.GetString("Js", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to $(function ()
        ///{
        ///    $(&apos;#newgame-playto&gt;input&apos;).click(function ()
        ///    {
        ///        var curVal = +$(&apos;#newgame-playto&gt;input:checked&apos;).val();
        ///        var newVals = curVal &lt; 3 ? [1, 2, 3, 4, 5] : [Math.max(1, Math.floor(curVal / 2)), curVal - 1, curVal, curVal + 1, curVal * 2];
        ///        for (var i = 0; i &lt; 5; i++)
        ///        {
        ///            $(&apos;#newgame-playto-&apos; + i).val(newVals[i]);
        ///            $(&apos;#newgame-playto-label-&apos; + i + &apos; .text&apos;).text(newVals[i]);
        ///        }
        ///        $(&apos;#newgame-playto-&apos; + newVals.indexOf [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string JsMain {
            get {
                return ResourceManager.GetString("JsMain", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to $(function ()
        ///{
        ///    function makeArrow(source, dest)
        ///    {
        ///        var pieceSize = 5; // vw
        ///
        ///        function midPoint(elem)
        ///        {
        ///            var tongue = +elem.data(&apos;tongue&apos;);
        ///            return {
        ///                left: leftFromTongue(tongue) + pieceSize / 2,
        ///                top: topFromTongue(tongue, +elem.data(&apos;index&apos;), +elem.data(&apos;num&apos;)) + pieceSize / 2
        ///            };
        ///        }
        ///
        ///        var srcPos = midPoint($(source)), dstPos = midPoint($(dest));
        ///        var dx = dstPos.left - srcPo [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string JsPlay {
            get {
                return ResourceManager.GetString("JsPlay", resourceCulture);
            }
        }
    }
}
