﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
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
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
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
        ///   Looks up a localized string similar to @font-face {
        ///    font-family: &apos;Lato&apos;;
        ///    font-style: normal;
        ///    font-weight: 900;
        ///    src: local(&apos;Lato Black&apos;), local(&apos;Lato-Black&apos;), url(//fonts.gstatic.com/s/lato/v10/BVtM30trf7q_jfqYeHfjtA.woff) format(&apos;woff&apos;);
        ///}
        ///
        ///body, select, input, textarea {
        ///    font-family: &apos;Candara&apos;, &apos;Calibri&apos;, &apos;Tahoma&apos;, &apos;Verdana&apos;, &apos;Arial&apos;, sans-serif;
        ///}
        ///
        ///body {
        ///    margin: 0;
        ///    padding: 0;
        ///    background: hsl(27, 35%, 50%);
        ///    cursor: default;
        ///}
        ///
        ///    body * {
        ///        cursor: inherit;
        ///    }
        ///
        ///select, input  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string Css {
            get {
                return ResourceManager.GetString("Css", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to body {
        ///    background: hsl(27, 60%, 70%);
        ///}
        ///
        ///h1, h2 {
        ///    padding: .2em .6em;
        ///    background: hsl(27,60%,80%);
        ///    border-bottom: 2px solid hsl(27,30%,50%);
        ///}
        ///
        ///table {
        ///    border-collapse: collapse;
        ///}
        ///
        ///td, th {
        ///    padding: .2em .4em;
        ///    border: 1px solid hsl(27,30%,60%);
        ///    vertical-align: top;
        ///}
        ///
        ///th {
        ///    background: hsl(27,60%,75%);
        ///    text-align: left;
        ///}
        ///
        ///.lang-hashname {
        ///    font-weight: bold;
        ///    font-size: 200%;
        ///}
        ///
        ///.lang-name {
        ///    font-weight: bold;
        ///    font-size:  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string CssAdmin {
            get {
                return ResourceManager.GetString("CssAdmin", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] Favicon {
            get {
                object obj = ResourceManager.GetObject("Favicon", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] FaviconDebug {
            get {
                object obj = ResourceManager.GetObject("FaviconDebug", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /*! jQuery v3.7.0 | (c) OpenJS Foundation and other contributors | jquery.org/license */
        ///!function(e,t){&quot;use strict&quot;;&quot;object&quot;==typeof module&amp;&amp;&quot;object&quot;==typeof module.exports?module.exports=e.document?t(e,!0):function(e){if(!e.document)throw new Error(&quot;jQuery requires a window with a document&quot;);return t(e)}:t(e)}(&quot;undefined&quot;!=typeof window?window:this,function(ie,e){&quot;use strict&quot;;var oe=[],r=Object.getPrototypeOf,ae=oe.slice,g=oe.flat?function(e){return oe.flat.call(e)}:function(e){return oe.concat.apply([], [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string JQuery {
            get {
                return ResourceManager.GetString("JQuery", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to LiBackgammon = {
        ///    hashAdd: function(vals, obj)
        ///    {
        ///        if (!(vals instanceof Array))
        ///            vals = [vals];
        ///        for (var i = 0; i &lt; vals.length; i++)
        ///            if (LiBackgammon.hash.values.indexOf(vals[i]) === -1)
        ///                LiBackgammon.hash.values.push(vals[i]);
        ///        if (typeof obj === &quot;object&quot;)
        ///            for (var i in obj)
        ///                LiBackgammon.hash.dict[i] = obj[i];
        ///        LiBackgammon.setHash(LiBackgammon.hash.values, LiBackgammon.hash.dict);
        ///    },
        ///
        ///  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string Js {
            get {
                return ResourceManager.GetString("Js", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to $(function ()
        ///{
        ///    var totalStrings = 0;
        ///    for (var i in LiBackgammon.strings)
        ///        if (LiBackgammon.strings.hasOwnProperty(i))
        ///            totalStrings++;
        ///
        ///    $(&apos;tr.lang&apos;).each(function (_, e)
        ///    {
        ///        var data = LiBackgammon.translations[$(e).data(&apos;lang&apos;)];
        ///        var doneStrings = 0;
        ///        for (var i in data)
        ///            if (data.hasOwnProperty(i) &amp;&amp; LiBackgammon.strings.hasOwnProperty(i))
        ///                doneStrings++;
        ///
        ///        $(e).find(&apos;.lang-status&apos;).text(doneStrings === [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string JsAdmin {
            get {
                return ResourceManager.GetString("JsAdmin", resourceCulture);
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
        ///   Looks up a localized string similar to $(function()
        ///{
        ///    var sidebars = [&apos;chat&apos;, &apos;info&apos;, &apos;settings&apos;, &apos;translate&apos;, &apos;translating&apos;];
        ///
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
        ///        var srcPos = midPoin [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string JsPlay {
            get {
                return ResourceManager.GetString("JsPlay", resourceCulture);
            }
        }
    }
}
