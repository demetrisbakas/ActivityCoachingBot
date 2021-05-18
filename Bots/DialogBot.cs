// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.BotBuilderSamples.Dialogs;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Microsoft.BotBuilderSamples.Bots
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBot<T> : ActivityHandler
        where T : Dialog
    {
        protected readonly Dialog Dialog;
        protected readonly BotState ConversationState;
        protected readonly BotState UserState;
        protected readonly ILogger Logger;

        protected static ConcurrentDictionary<string, ConversationReference> _conversationReferences;

        public DialogBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger, ConcurrentDictionary<string, ConversationReference> conversationReferences)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            Logger = logger;

            _conversationReferences = conversationReferences;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            //var userName = turnContext.Activity.From.Name;
            //var userID = turnContext.Activity.From.Id;
            //await turnContext.SendActivityAsync($"Welcome - {userName}, {userID}");
            if (PersonalDetailsDialog.PersonalDetails?.UserID == null || turnContext.Activity.From.Id != PersonalDetailsDialog.PersonalDetails?.UserID)
            {
                PersonalDetailsDialog.PersonalDetails = new PersonalDetails();
                PersonalDetailsDialog.PersonalDetails.UserID = turnContext.Activity.From.Id;
                PersonalDetailsDialog.PersonalDetails.Name = turnContext.Activity.From.Name;

                MainDialog.ReadFromDb = MainDialog.CosmosDBQuery.ReadAsync(new string[] { PersonalDetailsDialog.PersonalDetails.UserID }, cancellationToken);
                MainDialog.ClusteringData = MainDialog.QueryClusterDetailsAsync();
                MainDialog.Questionnaires = MainDialog.QueryQuestionnairesAsync();
            }

            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with Message Activity.");

            // Run the Dialog with the new message Activity.
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);

            AddConversationReference(turnContext.Activity as Activity);
        }

        //protected override Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        //{
        //    AddConversationReference(turnContext.Activity as Activity);

        //    return base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        //}

        protected static void AddConversationReference(Activity activity)
        {
            var conversationReference = activity.GetConversationReference();
            _conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
        }
    }
}
