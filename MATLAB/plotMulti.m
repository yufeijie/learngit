function plotMulti(headline, xLabelName, yLabelName, total, number, id, size, xData, yData)
% plotPareto 画多个子图
% headline 大标题
% xLabelName yLabelName 坐标轴名
% total 总子图数
% number 单个子图内曲线数
% id 子图对应ID
% size 曲线数据总数
% data 曲线数据

figure(10)

total = double(total);
for i = 1:total
    subplot(total, 1, i);
    for j = 1:number(i)
        k = id(i, j);
        plot(xData(k, 1:size(k)), yData(k, 1:size(k)));
        hold on
    end
    xlabel(xLabelName)
    ylabel(yLabelName)
end

suptitle(headline)
end