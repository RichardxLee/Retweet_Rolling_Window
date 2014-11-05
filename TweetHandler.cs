using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;

namespace Retweet_Rolling_Window
{
    class TweetHandler
    {
        
        public string getTweets(string _resource_url, string _query)
        {
            // code for this function is basically from Twitter, basics on accessing data patterns, no optimization is used
            // oauth application keys
            var oauth_token = "29463499-9Og6hxW4HqFxcQyIrAdmLpbAnrwIk290ghOE0ez5f";
            var oauth_token_secret = "elXVYJRFmFFit3PiVTmI9eU0IvHqqD7H4yeEmClJ8c";
            var oauth_consumer_key = "8AqQCy7umStCyNN356v7fw";
            var oauth_consumer_secret = "vOvKV1QwuS1AeKPMIvJqErBxW7i1N12OL4UY2tNMs0c";

            // oauth implementation details
            var oauth_version = "1.0";
            var oauth_signature_method = "HMAC-SHA1";

            // unique request details
            var oauth_nonce = Convert.ToBase64String(new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString()));
            var timeSpan = DateTime.UtcNow
                - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var oauth_timestamp = Convert.ToInt64(timeSpan.TotalSeconds).ToString();


            // create oauth signature
            var baseFormat = "oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method={2}" +
                            "&oauth_timestamp={3}&oauth_token={4}&oauth_version={5}&q={6}";

            var baseString = string.Format(baseFormat,
                                        oauth_consumer_key,
                                        oauth_nonce,
                                        oauth_signature_method,
                                        oauth_timestamp,
                                        oauth_token,
                                        oauth_version,
                                        Uri.EscapeDataString(_query)
                                        );

            baseString = string.Concat("GET&", Uri.EscapeDataString(_resource_url), "&", Uri.EscapeDataString(baseString));

            var compositeKey = string.Concat(Uri.EscapeDataString(oauth_consumer_secret),
                                    "&", Uri.EscapeDataString(oauth_token_secret));

            string oauth_signature;
            using (HMACSHA1 hasher = new HMACSHA1(ASCIIEncoding.ASCII.GetBytes(compositeKey)))
            {
                oauth_signature = Convert.ToBase64String(
                    hasher.ComputeHash(ASCIIEncoding.ASCII.GetBytes(baseString)));
            }

            // create the request header
            var headerFormat = "OAuth oauth_nonce=\"{0}\", oauth_signature_method=\"{1}\", " +
                               "oauth_timestamp=\"{2}\", oauth_consumer_key=\"{3}\", " +
                               "oauth_token=\"{4}\", oauth_signature=\"{5}\", " +
                               "oauth_version=\"{6}\"";

            var authHeader = string.Format(headerFormat,
                                    Uri.EscapeDataString(oauth_nonce),
                                    Uri.EscapeDataString(oauth_signature_method),
                                    Uri.EscapeDataString(oauth_timestamp),
                                    Uri.EscapeDataString(oauth_consumer_key),
                                    Uri.EscapeDataString(oauth_token),
                                    Uri.EscapeDataString(oauth_signature),
                                    Uri.EscapeDataString(oauth_version)
                            );

            //ServicePointManager.Expect100Continue = false;

            // make the request
            string postBody = "q=" + Uri.EscapeDataString(_query);//
            _resource_url += "?" + postBody;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_resource_url);
            request.Headers.Add("Authorization", authHeader);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";

            string data = "";
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                data = reader.ReadToEnd();
            }
            catch
            {
                //if got null response, wait for next cycle
            }
            
            return data;
        }

        public void processTweet(string _data, int _time_window, ref Hashtable _ht, ref SortedList<DateTime, string> _to_be_deleted, ref Utilities _u)
        {
            //parse tweet using json parser
            if (_data == "")
                return;
            dynamic obj = JsonConvert.DeserializeObject(_data);
            foreach (var data in obj)
            {
                string msg = "", time = "";
                try
                {
                    msg = data.status.text;
                    time = data.created_at;
                }
                catch
                {
                    //if tweet data incomplete, do nothing
                }

                if ((msg != "") && (time != ""))
                {
                    //if tweet data is good, check time constraint, and store into hashtable
                    //Console.WriteLine("msg: " + msg + "\n" + "time: " + time);

                    DateTime dt = DateTime.ParseExact(time,
                                  "ddd MMM dd HH:mm:ss zzz yyyy",
                                  CultureInfo.InvariantCulture,
                                  DateTimeStyles.AdjustToUniversal);

                    int time_left_in_seconds = _u.checkTime(dt, _time_window);

                    if (time_left_in_seconds > 0)
                    {
                        //update hashtable
                        if (_ht.ContainsKey(msg))
                        {
                            _ht[msg] = ((int)_ht[msg]) + 1;
                        }
                        else
                        {
                            _ht.Add(msg, 1);
                        }

                        //update to be deleted list: so that when the times comes where the data is about to expire, this entry would be deleted automatically by to_be_deleted list
                        DateTime time_deletion = _u.calculateTime(time_left_in_seconds);
                        _to_be_deleted.Add(time_deletion, msg);
                    }
                }
            }
        }

    }
}
