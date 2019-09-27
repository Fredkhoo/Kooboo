import { Configuration } from "webpack";
import { VueLoaderPlugin } from "vue-loader";
import path from "path";

export default function(env: any): Configuration {
  return {
    entry: {
      "meta-render": "./src/index.ts"
    },
    output: {
      path: path.resolve("../_Admin/Scripts/kooboo-meta-render"),
      filename: "[name].min.js"
    },
    mode: env.NODE_ENV,
    watch: env.NODE_ENV == "development",
    resolve: {
      extensions: [".ts"],
      alias: {
        "@": path.resolve(__dirname, "src")
      }
    },
    module: {
      rules: [
        {
          test: /\.vue$/,
          loader: "vue-loader"
        },
        {
          test: /\.tsx?$/,
          loader: "ts-loader",
          exclude: /node_modules/,
          options: {
            appendTsSuffixTo: [/\.vue$/]
          }
        }
      ]
    },
    plugins: [new VueLoaderPlugin()]
  };
}
