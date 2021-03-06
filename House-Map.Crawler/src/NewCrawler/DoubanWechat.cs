using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using RestSharp;
using System.Data;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using Dapper;
using AngleSharp.Dom;
using HouseMap.Dao;
using HouseMap.Dao.DBEntity;
using HouseMap.Crawler.Common;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using HouseMap.Models;
using HouseMap.Common;
using System.IO;
using Microsoft.Extensions.Options;

namespace HouseMap.Crawler
{

    public class DoubanWechat : NewBaseCrawler
    {
        private readonly AppSettings _appSettings;
        public DoubanWechat(NewHouseDapper houseDapper, ConfigDapper configDapper, IOptions<AppSettings> configuration) : base(houseDapper, configDapper)
        {
            this.Source = SourceEnum.DoubanWechat;
            _appSettings = configuration.Value;
        }


        public override string GetJsonOrHTML(DBConfig config, int page)
        {
            return GetTopics(config, page);
        }

        private string GetTopics(DBConfig config, int page)
        {
            try
            {
                var client = new RestClient($"{_appSettings.NodeProxyHost}/topics?city={config.City}&page={page}&limit=30");
                var request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);
                return response.Content;
            }
            catch (Exception ex)
            {
                LogHelper.Error("DoubanWechat", ex, config);
                return string.Empty;
            }
        }

        private string GetTopicDetail(string topicId)
        {
            try
            {
                var client = new RestClient($"{_appSettings.NodeProxyHost}/topics/{topicId}");
                var request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);
                return response.Content;
            }
            catch (Exception ex)
            {
                LogHelper.Error("DoubanWechat", ex, topicId);
                return string.Empty;
            }
        }
        public override List<DBHouse> ParseHouses(DBConfig config, string data)
        {
            var houses = new List<DBHouse>();
            var topics = JToken.Parse(data)?["topics"];
            foreach (var topic in topics)
            {
                DBHouse house = new DBHouse();
                var onlineURL = $"https://fang.douban.com/topics/{topic["id"].ToString()}/";
                house.Id = Tools.GetUUId();
                house.OnlineURL = onlineURL;
                var topicDetailJson = GetTopicDetail(topic["id"].ToString());
                if (!string.IsNullOrEmpty(topicDetailJson))
                {
                    var topicDetail = JToken.Parse(topicDetailJson)?["data"];
                    house.Title = topicDetail["title"].ToString();
                    house.Price = topicDetail["rent_fee"].ToObject<int>();
                    house.Location = topicDetail["district_tag"]["name"].ToString();
                    house.Latitude = topicDetail["district_tag"]["latitude"].ToString();
                    house.Longitude = topicDetail["district_tag"]["longitude"].ToString();
                    house.PubTime = topicDetail["create_time"].ToObject<DateTime>();
                    house.RentType = ConvertRentType(topicDetail["rent_type"].ToObject<int>(), topicDetail["house_type_display"].ToString());
                    house.Text = topicDetail["description"].ToString();
                    house.Labels = string.Join("|", topicDetail["labels"].ToObject<List<string>>());
                    house.Source = SourceEnum.DoubanWechat.GetSourceName();
                    house.JsonData = topicDetailJson;
                    house.City = config.City;
                    house.CreateTime = DateTime.Now;
                    house.Tags = $"{topicDetail["house_type_display"]?.ToString()}|{topicDetail["direction_display"]?.ToString()}|{topicDetail["topic_kind_display"]?.ToString()}";
                    house.PicURLs = JsonConvert.SerializeObject(topicDetail["photos"].Select(item => item["large_picture_url"].ToString()));
                    houses.Add(house);
                }
            }
            return houses;

        }

        private int ConvertRentType(int sourceRentType, string houseTypeDisplay)
        {
            if (houseTypeDisplay == "一居室")
            {
                return (int)RentTypeEnum.OneRoom;
            }
            if (sourceRentType == 2)
            {
                return (int)RentTypeEnum.Shared;
            }
            else if (sourceRentType == 1)
            {
                return (int)RentTypeEnum.AllInOne;
            }
            return (int)RentTypeEnum.Undefined;
        }








    }


}