using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    public sealed class CreateNewGameResult
    {
        public string PublicID { get; private set; }
        public string WhiteToken { get; private set; }
        public string BlackToken { get; private set; }

        public CreateNewGameResult(string publicID, string whiteToken, string blackToken)
        {
            PublicID = publicID;
            WhiteToken = whiteToken;
            BlackToken = blackToken;
        }
    }
}
