using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Retweet_Rolling_Window
{
    class Program
    {
        
        static void Main(string[] args)
        {
            //declaring global variables
            int time_window = 60 * 60 * 24; //1 day window
            int sleep_time = 5; //1 second
            //string url = "https://stream.twitter.com/1.1/statuses/sample.json";
            //string url = "https://api.twitter.com/1.1/statuses/user_timeline.json";
            string url = "https://api.twitter.com/1.1/users/search.json";
            //hashtable key:msg value:count
            Hashtable ht = new Hashtable();
            //sorted list that stores when to delete the next item (string=msg, int=time)
            SortedList<DateTime, string> to_be_deleted = new SortedList<DateTime, string>();
            //initialize tweethandler and utilities
            TweetHandler th = new TweetHandler();
            Utilities util = new Utilities();
            string q = "retweet";

            while (true) {
                
                //check current time and see if it is equal to the first element of need to be deleted 
                DateTime now = DateTime.Now;
                if (to_be_deleted.Count > 0)
                {
                    if ((to_be_deleted.ElementAt(0).Key <= now)&&(to_be_deleted.ElementAt(0).Key >= now.Subtract(TimeSpan.FromSeconds(sleep_time))))
                    {
                        //if now time == first element of to be deleted, , decrement hashtable, and delete element from list
                        ht[to_be_deleted.ElementAt(0).Value] = ((int)ht[to_be_deleted.ElementAt(0).Value]) - 1;
                        if (((int)ht[to_be_deleted.ElementAt(0).Value]) < 0)
                            ht.Remove(to_be_deleted.ElementAt(0).Value);
                        to_be_deleted.RemoveAt(0);
                    }
                }

                //get new tweets
                string data = th.getTweets(url, q);
                th.processTweet(data, time_window, ref ht, ref to_be_deleted, ref util);

                //output max 10 for rolling time window
                //could have implemented a max heap, but since in C#, it is as easy to do a dictionary, will choose the easier way :p
                var rank = new Dictionary<string, int>();
                foreach (DictionaryEntry en in ht)
                {
                    rank.Add((string)en.Key, (int)en.Value);
                }
                int count = 1;
                foreach (var item in rank.OrderByDescending(i => i.Key))
                {
                    Console.WriteLine(count + " msg:" + item.Key.ToString() + "retweet #:" + item.Value.ToString());
                    count++;
                    if (count > 10)
                        break;
                }

                //sleep 1 second
                Thread.Sleep(sleep_time);
                //Console.Clear();
            }
        }
    }
}