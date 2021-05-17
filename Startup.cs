// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples.Bots;
using Microsoft.BotBuilderSamples.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Create a global hashset for our ConversationReferences
            services.AddSingleton<ConcurrentDictionary<string, ConversationReference>>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // Register LUIS recognizer
            services.AddSingleton<ConnectionRecognizer>();

            // The AuthenticationDialog that will be run by the bot.
            services.AddSingleton<AuthenticationDialog>();

            // The UploadTipsOrQuestionnairesDialog that will be run by the bot.
            services.AddSingleton<UploadTipsOrQuestionnairesDialog>();

            // Register the PersonalDetailsDialog.
            services.AddSingleton<PersonalDetailsDialog>();

            // Register the TopFiveDialog.
            services.AddSingleton<TopFiveDialog>();

            // Register the QuestionnaireChoiceDialog.
            services.AddSingleton<QuestionnaireChoiceDialog>();

            // Register the QuestionnaireChoiceDialog.
            services.AddSingleton<ReenterDetailsDialog>();

            // Register the NumberOfTipsDialog.
            services.AddSingleton<NumberOfTipsDialog>();

            // Register the UploadTipsDialog.
            services.AddSingleton<UploadTipsDialog>();

            // Register the NameOfQuestionnaireDialog.
            services.AddSingleton<NameOfQuestionnaireDialog>();

            // Register the UploadQuestionnairesDialog.
            services.AddSingleton<UploadQuestionnairesDialog>();

            // The MainDialog that will be run by the bot.
            services.AddSingleton<MainDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            //services.AddTransient<IBot, DialogAndWelcomeBot<MainDialog>>();

            ////
            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, ProactiveBot>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();

            app.UseMvc();
        }
    }
}
