const config = {
  initiateLoginEndpoint: process.env.REACT_APP_START_LOGIN_PAGE_URL,
  clientId: process.env.REACT_APP_CLIENT_ID,
  apiEndpoint: process.env.REACT_APP_FUNC_ENDPOINT,
  apiName: process.env.REACT_APP_FUNC_NAME,
  bot: process.env.REACT_APP_BOT_ENDPOINT,
  defaultWavUrl: process.env.REACT_APP_BOT_DEFAULT_WAV,
};

export default config;
